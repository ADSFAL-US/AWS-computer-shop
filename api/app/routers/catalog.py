from decimal import Decimal

from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy import select
from sqlalchemy.orm import Session

from ..database import get_db
from ..deps import get_current_user
from ..models import Category, CrossSellOffer, Product
from ..schemas import CategoryOut, CrossSellOfferOut, ProductOut

router = APIRouter(prefix="/api", tags=["catalog"], dependencies=[Depends(get_current_user)])


@router.get("/categories", response_model=list[CategoryOut])
def list_categories(db: Session = Depends(get_db)) -> list[Category]:
    return list(db.scalars(select(Category).order_by(Category.id)))


@router.get("/products", response_model=list[ProductOut])
def list_products(
    search: str | None = None,
    category_id: int | None = None,
    tags: list[str] | None = Query(default=None),
    min_price: Decimal | None = None,
    max_price: Decimal | None = None,
    only_in_stock: bool = False,
    db: Session = Depends(get_db),
) -> list[Product]:
    query = select(Product)

    if search:
        query = query.where(Product.name.ilike(f"%{search}%"))
    if category_id is not None:
        query = query.where(Product.category_id == category_id)
    if tags:
        query = query.where(Product.tags.overlap(tags))
    if min_price is not None:
        query = query.where(Product.price >= min_price)
    if max_price is not None:
        query = query.where(Product.price <= max_price)
    if only_in_stock:
        query = query.where(Product.stock > 0)

    return list(db.scalars(query.order_by(Product.id)))

@router.get("/products/barcode/{barcode}", response_model=ProductOut)
def get_product_by_barcode(barcode: str, db: Session = Depends(get_db)) -> Product:
    product = db.scalar(select(Product).where(Product.barcode == barcode))
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )
    return product


@router.get("/products/{product_id}", response_model=ProductOut)
def get_product(product_id: int, db: Session = Depends(get_db)) -> Product:
    product = db.get(Product, product_id)
    if product is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Товар не найден"
        )
    return product


@router.get("/cross-sell-offers", response_model=list[CrossSellOfferOut])
def list_cross_sell_offers(db: Session = Depends(get_db)) -> list[CrossSellOffer]:
    # Предложения без привязанного товара или с нулевым остатком не продаём
    return list(
        db.scalars(
            select(CrossSellOffer)
            .outerjoin(Product, Product.id == CrossSellOffer.product_id)
            .where(
                (CrossSellOffer.product_id.is_(None)) | (Product.stock > 0)
            )
            .order_by(CrossSellOffer.id)
        )
    )
