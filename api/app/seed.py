"""Создание учётной записи администратора при первом запуске.

Демо-каталог не засеивается: товары добавляются через админку
(вручную, загрузкой изображений или импортом base64).
"""

import logging

from sqlalchemy import select
from sqlalchemy.orm import Session

from .config import settings
from .models import User
from .security import hash_password

logger = logging.getLogger(__name__)


def seed_database(db: Session) -> None:
    _seed_admin(db)


def _seed_admin(db: Session) -> None:
    if db.scalar(select(User).limit(1)) is not None:
        return

    db.add(
        User(
            login=settings.admin_login,
            password_hash=hash_password(settings.admin_password),
            full_name=settings.admin_full_name,
            role="Admin",
        )
    )
    db.flush()
    logger.info("Seeded default admin user '%s'", settings.admin_login)
