"""Админские операции с товарами: CRUD, изображения, импорт."""

import json
import logging

from fastapi import APIRouter, Depends, HTTPException, Request, status
from sqlalchemy import func, select
from sqlalchemy.orm import Session

from .. import catalog_service as svc
from .. import images
from ..config import settings
from ..database import get_db
from ..deps import require_admin
from ..models import AuditLogEntry, Category, Product, User
from ..schemas import (
    ImageMetaOut,
    ImageUploadIn,
    ImportProductIn,
    ImportRequest,
    ImportResultItem,
    ImportResultOut,
    ProductCreate,
    ProductOut,
    ProductUpdate,
)
from .images import image_meta

logger = logging.getLogger(__name__)

admin_products_router = APIRouter(prefix="/products", tags=["admin-products"])

IMAGE_FIELDS = {"image_file", "image_token", "image_updated_at"}


def _log(db: Session, actor: User, action: str, details: str) -> None:
    db.add(
        AuditLogEntry(
            user_id=actor.id,
            user_name=actor.full_name or actor.login,
            action=action,
            details=details[:512],
        )
    )


def _category_id_for(db: Session, name: str | None) -> int:
    """Находит категорию по имени или создаёт новую."""
    if not name or not name.strip():
        first = db.scalar(select(Category).order_by(Category.id))
        if first is None:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Сначала создайте хотя бы одну категорию",
            )
        return first.id

    cleaned = name.strip()
    category = db.scalar(select(Category).where(func.lower(Category.name) == cleaned.lower()))
    if category is None:
        category = Category(name=cleaned)
        db.add(category)
        db.flush()
    return category.id


def _validate_category(db: Session, category_id: int) -> None:
    if db.get(Category, category_id) is None:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Категория не найдена",
        )


def _known_image_files(db: Session) -> set[str]:
    return {name for (name,) in db.execute(select(Product.image_file)) if name}

def _image_meta(product: Product, request: Request) -> ImageMetaOut:
    return image_meta(product, request)

# ---------- import ----------


@admin_products_router.post("/import", response_model=ImportResultOut)
def import_products(
    payload: ImportRequest,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> ImportResultOut:
    try:
        raw = svc.decode_base64(payload.payload)
    except Exception:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Не удалось декодировать base64",
        )

    try:
        text = raw.decode("utf-8-sig")
    except UnicodeDecodeError:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Внутри base64 должен быть UTF-8 JSON",
        )

    try:
        data = json.loads(text)
    except json.JSONDecodeError as exc:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=f"Некорректный JSON: {exc.msg} (строка {exc.lineno})",
        )

    if isinstance(data, dict):
        # Поддерживаем и «конверт» {products: [...]}, и одиночный товар
        if isinstance(data.get("products"), list):
            data = data["products"]
        elif isinstance(data.get("items"), list):
            data = data["items"]
        else:
            data = [data]
    if not isinstance(data, list):
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Ожидался JSON-массив товаров",
        )

    results: list[ImportResultItem] = []
    created = updated = failed = 0

    for index, entry in enumerate(data):
        if not isinstance(entry, dict):
            failed += 1
            results.append(
                ImportResultItem(index=index, status="error", message="Элемент не объект")
            )
            continue

        try:
            item = ImportProductIn.model_validate(entry)
        except Exception as exc:  # pydantic validation
            failed += 1
            results.append(
                ImportResultItem(
                    index=index,
                    name=str(entry.get("name", "")),
                    status="error",
                    message=f"Ошибка валидации: {exc}",
                )
            )
            continue

        try:
            product, was_created = _upsert_imported(db, item)
            image_saved = False
            image_error = ""
            if item.image_url or item.image_base64:
                image_saved, image_error = _attach_image(product, item)

            db.commit()
            db.refresh(product)
            if was_created:
                created += 1
            else:
                updated += 1
            message = image_error
            results.append(
                ImportResultItem(
                    index=index,
                    sku=product.sku,
                    name=product.name,
                    status="created" if was_created else "updated",
                    message=message,
                    product_id=product.id,
                    image_saved=image_saved,
                )
            )
        except HTTPException as exc:
            db.rollback()
            images.cleanup_orphans(_known_image_files(db))
            failed += 1
            results.append(
                ImportResultItem(
                    index=index,
                    name=item.name,
                    status="error",
                    message=str(exc.detail),
                )
            )
        except Exception as exc:
            db.rollback()
            images.cleanup_orphans(_known_image_files(db))
            logger.exception("Import failed for item %s", index)
            failed += 1
            results.append(
                ImportResultItem(
                    index=index,
                    name=item.name,
                    status="error",
                    message=str(exc),
                )
            )

    _log(
        db,
        actor,
        "PRODUCT_IMPORT",
        f"Импорт товаров: создано {created}, обновлено {updated}, ошибок {failed}",
    )
    db.commit()

    return ImportResultOut(
        total=len(data), created=created, updated=updated, failed=failed, items=results
    )


def _upsert_imported(db: Session, item: ImportProductIn) -> tuple[Product, bool]:
    product: Product | None = None
    if item.sku:
        product = db.scalar(select(Product).where(Product.sku == item.sku))
    if product is None and item.barcode:
        product = db.scalar(select(Product).where(Product.barcode == item.barcode))
    if product is None:
        # Без артикула/штрихкода повторный импорт не должен плодить дубли
        product = db.scalar(
            select(Product).where(func.lower(Product.name) == item.name.strip().lower())
        )

    created = product is None
    if product is None:
        category_id = (
            item.category_id
            if item.category_id and db.get(Category, item.category_id)
            else _category_id_for(db, item.category)
        )
        product = Product(
            sku=item.sku or svc.next_sku(db, item.name),
            barcode=item.barcode or svc.next_barcode(db),
            name=item.name,
            category_id=category_id,
            price=item.price,
            stock=item.stock,
        )
        db.add(product)
        db.flush()

    product.name = item.name
    if item.description:
        product.description = item.description
    if item.category_id or item.category:
        product.category_id = (
            item.category_id
            if item.category_id and db.get(Category, item.category_id)
            else _category_id_for(db, item.category)
        )
    product.price = item.price
    product.stock = item.stock
    if item.tags:
        product.tags = [t[:32] for t in item.tags][:20]
    if item.icon:
        product.icon = item.icon[:16]
    if item.image_gradient:
        product.image_gradient = item.image_gradient[:64]
    product.is_cross_sell = item.is_cross_sell
    if item.cross_sell_category:
        product.cross_sell_category = item.cross_sell_category[:32]

    db.flush()
    svc.sync_cross_sell_offer(db, product)
    svc.drop_image_if_out_of_stock(product)
    return product, created


def _attach_image(product: Product, item: ImportProductIn) -> tuple[bool, str]:
    if item.image_base64:
        data = svc.try_decode_image(item.image_base64)
        if data is None:
            return False, "Некорректный base64 изображения"
        if svc.set_image(product, data):
            return True, ""
        return False, "Неподдерживаемый формат изображения"

    assert item.image_url
    try:
        data = images.download_remote(item.image_url)
    except Exception as exc:
        return False, f"Не удалось скачать изображение: {exc}"

    if svc.set_image(product, data, item.image_url):
        return True, ""
    return False, "Неподдерживаемый формат изображения"


# ---------- CRUD ----------


@admin_products_router.get("", response_model=list[ProductOut])
def list_products(db: Session = Depends(get_db)) -> list[Product]:
    return list(db.scalars(select(Product).order_by(Product.id)))


@admin_products_router.post("", response_model=ProductOut, status_code=status.HTTP_201_CREATED)
def create_product(
    payload: ProductCreate,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> Product:
    _validate_category(db, payload.category_id)

    sku = payload.sku.strip() or svc.next_sku(db, payload.name)
    barcode = payload.barcode.strip() or svc.next_barcode(db)

    if db.scalar(select(Product).where(Product.sku == sku)) is not None:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="Артикул уже используется"
        )
    if db.scalar(select(Product).where(Product.barcode == barcode)) is not None:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="Штрихкод уже используется"
        )

    product = Product(
        sku=sku,
        barcode=barcode,
        name=payload.name,
        description=payload.description,
        category_id=payload.category_id,
        price=payload.price,
        stock=payload.stock,
        tags=[t[:32] for t in payload.tags][:20],
        image_gradient=payload.image_gradient,
        icon=payload.icon,
        is_cross_sell=payload.is_cross_sell,
        cross_sell_category=payload.cross_sell_category,
    )
    db.add(product)
    db.flush()
    svc.sync_cross_sell_offer(db, product)
    _log(db, actor, "PRODUCT_CREATE", f"Создан товар {product.name} ({product.sku})")
    db.commit()
    db.refresh(product)
    return product


@admin_products_router.put("/{product_id}", response_model=ProductOut)
def update_product(
    product_id: int,
    payload: ProductUpdate,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> Product:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )

    if payload.category_id is not None:
        _validate_category(db, payload.category_id)
        product.category_id = payload.category_id

    try:
        svc.ensure_unique(db, product, sku=payload.sku or None, barcode=payload.barcode or None)
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=str(exc))

    if payload.name is not None:
        product.name = payload.name
    if payload.description is not None:
        product.description = payload.description
    if payload.price is not None:
        product.price = payload.price
    if payload.stock is not None:
        product.stock = payload.stock
    if payload.tags is not None:
        product.tags = [t[:32] for t in payload.tags][:20]
    if payload.image_gradient is not None:
        product.image_gradient = payload.image_gradient
    if payload.icon is not None:
        product.icon = payload.icon
    if payload.is_cross_sell is not None:
        product.is_cross_sell = payload.is_cross_sell
    if payload.cross_sell_category is not None:
        product.cross_sell_category = payload.cross_sell_category

    svc.sync_cross_sell_offer(db, product)
    svc.drop_image_if_out_of_stock(product)
    _log(db, actor, "PRODUCT_UPDATE", f"Изменён товар {product.name} ({product.sku})")
    db.commit()
    db.refresh(product)
    return product


@admin_products_router.delete("/{product_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_product(
    product_id: int,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> None:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )

    images.delete_image(product.image_file)
    _log(db, actor, "PRODUCT_DELETE", f"Удалён товар {product.name} ({product.sku})")
    db.delete(product)
    db.commit()


# ---------- images ----------


@admin_products_router.post("/{product_id}/image", response_model=ImageMetaOut)
def upload_product_image(
    product_id: int,
    payload: ImageUploadIn,
    request: Request,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> ImageMetaOut:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )

    data: bytes | None
    source = ""
    if svc.looks_like_url(payload.data):
        try:
            data = images.download_remote(payload.data)
        except Exception as exc:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=f"Не удалось скачать изображение: {exc}",
            )
        source = payload.data
    else:
        data = svc.try_decode_image(payload.data)

    if not data:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Некорректные данные изображения",
        )
    if len(data) > settings.image_max_bytes:
        raise HTTPException(
            status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
            detail="Изображение слишком большое",
        )
    if not svc.set_image(product, data, source):
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Неподдерживаемый формат изображения (нужен JPEG/PNG/GIF/WebP/BMP)",
        )

    _log(db, actor, "PRODUCT_IMAGE", f"Обновлено изображение товара {product.name}")
    db.commit()
    db.refresh(product)
    return _image_meta(product, request)


@admin_products_router.delete("/{product_id}/image", status_code=status.HTTP_204_NO_CONTENT)
def delete_product_image(
    product_id: int,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> None:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )

    svc.clear_image(product)
    _log(db, actor, "PRODUCT_IMAGE_DELETE", f"Удалено изображение товара {product.name}")
    db.commit()


@admin_products_router.post("/maintenance/cleanup-images")
def cleanup_images(db: Session = Depends(get_db)) -> dict[str, int]:
    """Удаляет с диска файлы, на которые не ссылается ни один товар."""
    known = {name for (name,) in db.execute(select(Product.image_file)) if name}
    removed = images.cleanup_orphans(known)
    return {"removed": removed}
