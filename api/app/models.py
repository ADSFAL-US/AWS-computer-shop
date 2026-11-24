from datetime import datetime
from decimal import Decimal

from sqlalchemy import DateTime, ForeignKey, Integer, Numeric, String, func
from sqlalchemy.dialects.postgresql import ARRAY
from sqlalchemy.orm import Mapped, mapped_column, relationship

from .database import Base


class User(Base):
    __tablename__ = "users"

    id: Mapped[int] = mapped_column(primary_key=True)
    login: Mapped[str] = mapped_column(String(64), unique=True, index=True)
    password_hash: Mapped[str] = mapped_column(String(255))
    full_name: Mapped[str] = mapped_column(String(128), default="")
    role: Mapped[str] = mapped_column(String(16), default="Cashier")
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now()
    )


class Category(Base):
    __tablename__ = "categories"

    id: Mapped[int] = mapped_column(primary_key=True)
    name: Mapped[str] = mapped_column(String(64), unique=True)
    icon: Mapped[str] = mapped_column(String(16), default="")


class Product(Base):
    __tablename__ = "products"

    id: Mapped[int] = mapped_column(primary_key=True)
    sku: Mapped[str] = mapped_column(String(32), unique=True, index=True)
    barcode: Mapped[str] = mapped_column(String(32), unique=True, index=True)
    name: Mapped[str] = mapped_column(String(128), index=True)
    category_id: Mapped[int] = mapped_column(ForeignKey("categories.id"))
    price: Mapped[Decimal] = mapped_column(Numeric(12, 2))
    stock: Mapped[int] = mapped_column(Integer, default=0)
    tags: Mapped[list[str]] = mapped_column(ARRAY(String(32)), default=list)
    description: Mapped[str] = mapped_column(String(512), default="")
    image_gradient: Mapped[str] = mapped_column(String(64), default="")
    icon: Mapped[str] = mapped_column(String(16), default="")

    # Файл изображения на диске: имя вида "{id}-{token}.{ext}" и версия для кеша
    image_file: Mapped[str] = mapped_column(String(255), default="")
    image_token: Mapped[str] = mapped_column(String(32), default="")
    image_updated_at: Mapped[datetime | None] = mapped_column(
        DateTime(timezone=True), nullable=True
    )

    # Признак дополнительного предложения (кросс-продажа) в кассе
    is_cross_sell: Mapped[bool] = mapped_column(default=False)
    cross_sell_category: Mapped[str] = mapped_column(String(32), default="accessory")

    category: Mapped[Category] = relationship(lazy="joined")

    @property
    def has_image(self) -> bool:
        return bool(self.image_file and self.image_token)


class Order(Base):
    __tablename__ = "orders"

    id: Mapped[int] = mapped_column(primary_key=True)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), index=True
    )
    cashier_id: Mapped[int] = mapped_column(ForeignKey("users.id"))
    discount: Mapped[Decimal] = mapped_column(Numeric(12, 2), default=0)
    total: Mapped[Decimal] = mapped_column(Numeric(12, 2), default=0)
    status: Mapped[str] = mapped_column(String(16), default="Paid")

    items: Mapped[list["OrderItem"]] = relationship(
        back_populates="order",
        cascade="all, delete-orphan",
        order_by="OrderItem.id",
        lazy="selectin",
    )


class OrderItem(Base):
    __tablename__ = "order_items"

    id: Mapped[int] = mapped_column(primary_key=True)
    order_id: Mapped[int] = mapped_column(ForeignKey("orders.id", ondelete="CASCADE"))
    # Товар может быть удалён из каталога, но история заказа должна сохраниться:
    # название и цена зафиксированы в самом заказе, а ссылка обнуляется.
    product_id: Mapped[int | None] = mapped_column(
        ForeignKey("products.id", ondelete="SET NULL"), nullable=True
    )
    product_name: Mapped[str] = mapped_column(String(128))
    unit_price: Mapped[Decimal] = mapped_column(Numeric(12, 2))
    quantity: Mapped[int] = mapped_column(Integer)

    order: Mapped[Order] = relationship(back_populates="items")


class CrossSellOffer(Base):
    __tablename__ = "cross_sell_offers"

    id: Mapped[int] = mapped_column(primary_key=True)
    name: Mapped[str] = mapped_column(String(128))
    description: Mapped[str] = mapped_column(String(255), default="")
    price: Mapped[Decimal] = mapped_column(Numeric(12, 2))
    icon: Mapped[str] = mapped_column(String(16), default="")
    category: Mapped[str] = mapped_column(String(32), default="accessory")
    # Предложение всегда привязано к реальному товару, чтобы его можно было
    # положить в заказ и списать со склада.
    product_id: Mapped[int | None] = mapped_column(
        ForeignKey("products.id", ondelete="CASCADE"), nullable=True
    )


class AuditLogEntry(Base):
    __tablename__ = "audit_log"

    id: Mapped[int] = mapped_column(primary_key=True)
    timestamp: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), index=True
    )
    user_id: Mapped[int] = mapped_column(Integer)
    user_name: Mapped[str] = mapped_column(String(128), default="")
    action: Mapped[str] = mapped_column(String(64))
    details: Mapped[str] = mapped_column(String(512), default="")


class IpEntry(Base):
    __tablename__ = "ip_whitelist"

    id: Mapped[int] = mapped_column(primary_key=True)
    ip_address: Mapped[str] = mapped_column(String(64), unique=True)
    comment: Mapped[str] = mapped_column(String(255), default="")
    added_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now()
    )
