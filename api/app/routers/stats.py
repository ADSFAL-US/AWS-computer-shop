from datetime import date, datetime, time, timedelta, timezone, tzinfo
from decimal import Decimal

from fastapi import APIRouter, Depends
from sqlalchemy import func, select
from sqlalchemy.orm import Session

from ..database import get_db
from ..deps import get_current_user
from ..models import Order, OrderItem, Product, User
from ..schemas import ProductOut, SalesPointOut, StatsOut, TopProductOut

router = APIRouter(prefix="/api/stats", tags=["stats"], dependencies=[Depends(get_current_user)])


def _local_tz() -> tzinfo:
    return datetime.now().astimezone().tzinfo or timezone.utc


def _day_bounds(day: date) -> tuple[datetime, datetime]:
    start = datetime.combine(day, time.min, tzinfo=_local_tz())
    return start, start + timedelta(days=1)


@router.get("", response_model=StatsOut)
def get_stats(db: Session = Depends(get_db)) -> StatsOut:
    start, end = _day_bounds(date.today())
    revenue, orders_count = db.execute(
        select(func.coalesce(func.sum(Order.total), 0), func.count(Order.id)).where(
            Order.status == "Paid", Order.created_at >= start, Order.created_at < end
        )
    ).one()

    revenue = Decimal(revenue)
    avg_check = revenue / orders_count if orders_count else Decimal("0")
    products_in_stock = db.scalar(select(func.coalesce(func.sum(Product.stock), 0)))

    return StatsOut(
        revenue=revenue,
        avg_check=avg_check,
        orders_count=orders_count,
        products_in_stock=products_in_stock or 0,
    )


@router.get("/top-products", response_model=list[TopProductOut])
def get_top_products(limit: int = 5, db: Session = Depends(get_db)) -> list[TopProductOut]:
    rows = db.execute(
        select(OrderItem.product_id, func.sum(OrderItem.quantity).label("sold"))
        .join(Order, Order.id == OrderItem.order_id)
        .where(Order.status == "Paid")
        .group_by(OrderItem.product_id)
        .order_by(func.sum(OrderItem.quantity).desc())
        .limit(limit)
    ).all()

    result: list[TopProductOut] = []
    for product_id, sold in rows:
        # Товар мог быть удалён — такие позиции в топ не попадают
        if product_id is None:
            continue
        product = db.get(Product, product_id)
        if product is not None:
            result.append(
                TopProductOut(product=ProductOut.model_validate(product), quantity_sold=sold)
            )
    return result


@router.get("/sales", response_model=list[SalesPointOut])
def get_sales_last_7_days(db: Session = Depends(get_db)) -> list[SalesPointOut]:
    today = date.today()
    result: list[SalesPointOut] = []

    for offset in range(6, -1, -1):
        day = today - timedelta(days=offset)
        start, end = _day_bounds(day)
        revenue = db.scalar(
            select(func.coalesce(func.sum(Order.total), 0)).where(
                Order.status == "Paid", Order.created_at >= start, Order.created_at < end
            )
        )
        result.append(SalesPointOut(date=day.isoformat(), revenue=Decimal(revenue)))

    return result
