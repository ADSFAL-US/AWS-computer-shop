from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import func, select
from sqlalchemy.orm import Session

from ..database import get_db
from ..deps import require_admin
from ..models import AuditLogEntry, Category, IpEntry, User
from ..schemas import (
    AuditLogOut,
    CategoryOut,
    IpEntryCreate,
    IpEntryOut,
    UserCreate,
    UserOut,
    UserUpdate,
)
from ..security import hash_password
from .products_admin import admin_products_router

router = APIRouter(
    prefix="/api/admin", tags=["admin"], dependencies=[Depends(require_admin)]
)


def _log(db: Session, actor: User, action: str, details: str) -> None:
    db.add(
        AuditLogEntry(
            user_id=actor.id, user_name=actor.full_name or actor.login, action=action, details=details
        )
    )


# ---------- users ----------


@router.get("/users", response_model=list[UserOut])
def list_users(db: Session = Depends(get_db)) -> list[User]:
    return list(db.scalars(select(User).order_by(User.id)))


@router.post("/users", response_model=UserOut, status_code=status.HTTP_201_CREATED)
def create_user(
    payload: UserCreate,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> User:
    if db.scalar(select(User).where(User.login == payload.login)) is not None:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="Логин уже занят"
        )

    user = User(
        login=payload.login,
        password_hash=hash_password(payload.password),
        full_name=payload.full_name,
        role=payload.role,
    )
    db.add(user)
    _log(db, actor, "USER_CREATE", f"Создан пользователь {payload.login}")
    db.commit()
    db.refresh(user)
    return user


@router.put("/users/{user_id}", response_model=UserOut)
def update_user(
    user_id: int,
    payload: UserUpdate,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> User:
    user = db.get(User, user_id)
    if user is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Пользователь не найден"
        )

    if payload.login is not None and payload.login != user.login:
        if db.scalar(select(User).where(User.login == payload.login)) is not None:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT, detail="Логин уже занят"
            )
        user.login = payload.login

    if payload.password:
        user.password_hash = hash_password(payload.password)
    if payload.full_name is not None:
        user.full_name = payload.full_name
    if payload.role is not None:
        if payload.role != user.role:
            _log(
                db,
                actor,
                "ROLE_CHANGE",
                f"Изменена роль пользователя {user.login} с {user.role} на {payload.role}",
            )
        user.role = payload.role

    db.commit()
    db.refresh(user)
    return user


@router.delete("/users/{user_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_user(
    user_id: int,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> None:
    user = db.get(User, user_id)
    if user is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="Пользователь не найден"
        )
    if user.id == actor.id:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="Нельзя удалить самого себя"
        )

    _log(db, actor, "USER_DELETE", f"Удалён пользователь {user.login}")
    db.delete(user)
    db.commit()


# ---------- audit ----------


@router.get("/audit-log", response_model=list[AuditLogOut])
def list_audit_log(db: Session = Depends(get_db)) -> list[AuditLogEntry]:
    return list(db.scalars(select(AuditLogEntry).order_by(AuditLogEntry.timestamp.desc())))


# ---------- ip whitelist ----------


@router.get("/ip-whitelist", response_model=list[IpEntryOut])
def list_ip_whitelist(db: Session = Depends(get_db)) -> list[IpEntry]:
    return list(db.scalars(select(IpEntry).order_by(IpEntry.id)))


@router.post("/ip-whitelist", response_model=IpEntryOut, status_code=status.HTTP_201_CREATED)
def add_ip_entry(
    payload: IpEntryCreate,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> IpEntry:
    if db.scalar(select(IpEntry).where(IpEntry.ip_address == payload.ip_address)) is not None:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="IP-адрес уже добавлен"
        )

    entry = IpEntry(ip_address=payload.ip_address, comment=payload.comment)
    db.add(entry)
    _log(db, actor, "IP_WHITELIST_ADD", f"Добавлен IP-адрес {payload.ip_address}")
    db.commit()
    db.refresh(entry)
    return entry


@router.delete("/ip-whitelist/{entry_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_ip_entry(
    entry_id: int,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> None:
    entry = db.get(IpEntry, entry_id)
    if entry is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="IP-адрес не найден"
        )

    _log(db, actor, "IP_WHITELIST_REMOVE", f"Удалён IP-адрес {entry.ip_address}")
    db.delete(entry)
    db.commit()


# ---------- categories ----------


@router.get("/categories", response_model=list[CategoryOut])
def list_all_categories(db: Session = Depends(get_db)) -> list[Category]:
    return list(db.scalars(select(Category).order_by(Category.id)))


@router.post("/categories", response_model=CategoryOut, status_code=status.HTTP_201_CREATED)
def create_category(
    payload: dict,
    db: Session = Depends(get_db),
    actor: User = Depends(require_admin),
) -> Category:
    name = str(payload.get("name", "")).strip()
    if not name:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Укажите название категории"
        )
    if db.scalar(select(Category).where(func.lower(Category.name) == name.lower())) is not None:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="Категория уже существует"
        )

    category = Category(name=name, icon=str(payload.get("icon", "")).strip())
    db.add(category)
    _log(db, actor, "CATEGORY_CREATE", f"Создана категория {name}")
    db.commit()
    db.refresh(category)
    return category


# ---------- products ----------

router.include_router(admin_products_router)
