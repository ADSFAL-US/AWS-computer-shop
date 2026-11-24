from fastapi import APIRouter, Depends, HTTPException, Request, status
from fastapi.responses import FileResponse
from sqlalchemy.orm import Session

from .. import images
from ..database import get_db
from ..deps import get_current_user
from ..models import Product
from ..schemas import ImageMetaOut

router = APIRouter(
    prefix="/api/products", tags=["images"], dependencies=[Depends(get_current_user)]
)


def image_meta(product: Product, request: Request) -> ImageMetaOut:
    if not product.has_image:
        return ImageMetaOut(product_id=product.id, available=False)

    info = images.read_image(product.image_file)
    if info is None:
        # Файл пропал с диска — считаем, что изображения нет
        return ImageMetaOut(product_id=product.id, available=False)

    _, content_type = info
    return ImageMetaOut(
        product_id=product.id,
        available=True,
        token=product.image_token,
        download_url=str(request.url_for("download_product_image", product_id=product.id)),
        content_type=content_type,
    )


@router.get("/{product_id}/image", response_model=ImageMetaOut)
def get_product_image_meta(
    product_id: int, request: Request, db: Session = Depends(get_db)
) -> ImageMetaOut:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )
    return image_meta(product, request)


@router.get("/{product_id}/image/file", name="download_product_image")
def download_product_image(product_id: int, db: Session = Depends(get_db)):
    product = db.get(Product, product_id)
    if product is None or not product.has_image:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Изображение не найдено"
        )

    info = images.read_image(product.image_file)
    if info is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Изображение не найдено"
        )

    path, content_type = info
    return FileResponse(
        path,
        media_type=content_type,
        headers={
            # Имя файла содержит токен версии, поэтому кеш можно держать долго
            "Cache-Control": "public, max-age=31536000, immutable",
            "ETag": f'"{product.image_token}"',
        },
    )
