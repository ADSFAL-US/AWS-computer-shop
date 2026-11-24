from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    database_url: str = (
        "postgresql+psycopg2://electroncash:electroncash@localhost:5432/electroncash"
    )
    jwt_secret: str = "change-me-in-production"
    jwt_algorithm: str = "HS256"
    jwt_expire_minutes: int = 720

    admin_login: str = "admin"
    admin_password: str = "admin"
    admin_full_name: str = "Администратор"

    # Куда складываются файлы изображений товаров (в docker — том)
    image_dir: str = "data/images"
    # Максимальный размер одного изображения при импорте/загрузке
    image_max_bytes: int = 10 * 1024 * 1024
    # Таймаут скачивания изображения по ссылке при импорте
    image_download_timeout: int = 15
    # Удалять файл изображения, когда товар распродан (stock == 0)
    delete_images_when_out_of_stock: bool = True


settings = Settings()
