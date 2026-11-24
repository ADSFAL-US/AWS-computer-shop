from datetime import datetime
from decimal import Decimal

from pydantic import BaseModel, ConfigDict, Field


class ORMModel(BaseModel):
    model_config = ConfigDict(from_attributes=True)


# ---------- auth ----------


class LoginRequest(BaseModel):
    login: str
    password: str


class UserOut(ORMModel):
    id: int
    login: str
    full_name: str
    role: str


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"
    user: UserOut


# ---------- catalog ----------


class CategoryOut(ORMModel):
    id: int
    name: str
    icon: str


class ProductOut(ORMModel):
    id: int
    sku: str
    barcode: str
    name: str
    description: str = ""
    category_id: int
    price: Decimal
    stock: int
    tags: list[str]
    image_gradient: str
    icon: str
    is_cross_sell: bool = False
    cross_sell_category: str = "accessory"
    has_image: bool = False
    image_token: str = ""
    image_updated_at: datetime | None = None


class ProductBase(BaseModel):
    sku: str = Field(default="", max_length=32)
    barcode: str = Field(default="", max_length=32)
    name: str = Field(min_length=1, max_length=128)
    description: str = ""
    category_id: int
    price: Decimal = Field(ge=0)
    stock: int = Field(ge=0)
    tags: list[str] = Field(default_factory=list)
    image_gradient: str = ""
    icon: str = ""
    is_cross_sell: bool = False
    cross_sell_category: str = "accessory"


class ProductCreate(ProductBase):
    pass


class ProductUpdate(BaseModel):
    sku: str | None = Field(default=None, max_length=32)
    barcode: str | None = Field(default=None, max_length=32)
    name: str | None = Field(default=None, min_length=1, max_length=128)
    description: str | None = None
    category_id: int | None = None
    price: Decimal | None = Field(default=None, ge=0)
    stock: int | None = Field(default=None, ge=0)
    tags: list[str] | None = None
    image_gradient: str | None = None
    icon: str | None = None
    is_cross_sell: bool | None = None
    cross_sell_category: str | None = None


class CrossSellOfferOut(ORMModel):
    id: int
    name: str
    description: str
    price: Decimal
    icon: str
    category: str
    product_id: int | None = None


# ---------- изображения ----------


class ImageMetaOut(BaseModel):
    """Метаданные изображения: есть ли оно и какая версия (для кеша)."""

    product_id: int
    available: bool
    token: str = ""
    download_url: str = ""
    content_type: str = ""


class ImageUploadIn(BaseModel):
    # base64 (можно с data:image/...;base64,) или http(s)-ссылка
    data: str = Field(min_length=1)
    filename: str = ""


class ImportProductIn(BaseModel):
    sku: str | None = None
    barcode: str | None = None
    name: str = Field(min_length=1)
    description: str = ""
    category: str | None = None
    category_id: int | None = None
    price: Decimal = Field(ge=0)
    stock: int = Field(default=0, ge=0)
    tags: list[str] = Field(default_factory=list)
    icon: str = ""
    image_gradient: str = ""
    image_url: str | None = None
    image_base64: str | None = None
    is_cross_sell: bool = False
    cross_sell_category: str = "accessory"


class ImportRequest(BaseModel):
    # base64 от JSON-списка товаров
    payload: str = Field(min_length=1)


class ImportResultItem(BaseModel):
    index: int
    sku: str = ""
    name: str = ""
    status: str  # created / updated / skipped / error
    message: str = ""
    product_id: int | None = None
    image_saved: bool = False


class ImportResultOut(BaseModel):
    total: int
    created: int
    updated: int
    failed: int
    items: list[ImportResultItem]


# ---------- orders ----------


class OrderItemIn(BaseModel):
    product_id: int
    quantity: int = Field(gt=0)


class OrderItemOut(ORMModel):
    product_id: int | None = None
    product_name: str
    unit_price: Decimal
    quantity: int


class OrderCreate(BaseModel):
    items: list[OrderItemIn] = Field(min_length=1)
    discount: Decimal = Decimal("0")
    status: str = "Paid"


class OrderOut(ORMModel):
    id: int
    created_at: datetime
    cashier_id: int
    items: list[OrderItemOut]
    discount: Decimal
    total: Decimal
    status: str


# ---------- stats ----------


class StatsOut(BaseModel):
    revenue: Decimal
    avg_check: Decimal
    orders_count: int
    products_in_stock: int


class TopProductOut(BaseModel):
    product: ProductOut
    quantity_sold: int


class SalesPointOut(BaseModel):
    date: str
    revenue: Decimal


# ---------- admin ----------


class UserCreate(BaseModel):
    login: str = Field(min_length=1, max_length=64)
    password: str = Field(min_length=1)
    full_name: str = ""
    role: str = "Cashier"


class UserUpdate(BaseModel):
    login: str | None = Field(default=None, min_length=1, max_length=64)
    password: str | None = None
    full_name: str | None = None
    role: str | None = None


class AuditLogOut(ORMModel):
    id: int
    timestamp: datetime
    user_id: int
    user_name: str
    action: str
    details: str


class IpEntryOut(ORMModel):
    id: int
    ip_address: str
    comment: str
    added_at: datetime


class IpEntryCreate(BaseModel):
    ip_address: str = Field(min_length=1, max_length=64)
    comment: str = ""
