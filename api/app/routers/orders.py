from decimal import Decimal

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.orm import Session

from .. import catalog_service, images
from ..database import get_db
from ..deps import get_current_user
from ..models import Order, OrderItem, Product, User
from ..schemas import OrderCreate, OrderOut

router = APIRouter(prefix="/api/orders", tags=["orders"])

VALID_STATUSES = {"Draft", "Paid", "Cancelled"}


@router.get("", response_model=list[OrderOut])
def list_orders(
    db: Session = Depends(get_db), _: User = Depends(get_current_user)
) -> list[Order]:
    return list(db.scalars(select(Order).order_by(Order.created_at.desc())))


@router.get("/{order_id}", response_model=OrderOut)
def get_order(
    order_id: int, db: Session = Depends(get_db), _: User = Depends(get_current_user)
) -> Order:
    order = db.get(Order, order_id)
    if order is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Заказ не найден"
        )
    return order


@router.post("", response_model=OrderOut, status_code=status.HTTP_201_CREATED)
def create_order(
    payload: OrderCreate,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> Order:
    if payload.status not in VALID_STATUSES:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=f"Недопустимый статус заказа: {payload.status}",
        )

    items: list[OrderItem] = []
    subtotal = Decimal("0")

    for item in payload.items:
        product = db.get(Product, item.product_id)
        if product is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Товар {item.product_id} не найден",
            )
        if product.stock < item.quantity:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail=f"Недостаточно товара «{product.name}» на складе",
            )
        items.append(
            OrderItem(
                product_id=product.id,
                product_name=product.name,
                unit_price=product.price,
                quantity=item.quantity,
            )
        )
        subtotal += product.price * item.quantity

    discount = min(payload.discount, subtotal)
    order = Order(
        cashier_id=user.id,
        discount=discount,
        total=subtotal - discount,
        status=payload.status,
        items=items,
    )

    if payload.status == "Paid":
        for item in items:
            product = db.get(Product, item.product_id)
            product.stock -= item.quantity
            # Товар кончился — картинку с диска убираем, клиент её тоже сбросит
            catalog_service.drop_image_if_out_of_stock(product)

    db.add(order)
    db.commit()
    db.refresh(order)
    return order
