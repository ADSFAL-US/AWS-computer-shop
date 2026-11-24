"""Хранилище изображений товаров.

Файлы лежат отдельными файлами на диске (каталог settings.image_dir).
В базе хранится только имя файла и токен версии, поэтому клиент может
кешировать картинку и проверять её актуальность без скачивания.
"""

import logging
import re
import secrets
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

from .config import settings

logger = logging.getLogger(__name__)

_SAFE_NAME = re.compile(r"^[A-Za-z0-9._-]+$")

# Магические байты -> расширение файла
_EXT_BY_MAGIC: list[tuple[bytes, str]] = [
    (b"\xff\xd8\xff", ".jpg"),
    (b"\x89PNG\r\n\x1a\n", ".png"),
    (b"GIF87a", ".gif"),
    (b"GIF89a", ".gif"),
    (b"BM", ".bmp"),
]

_EXT_BY_URL_SUFFIX = {".jpg": ".jpg", ".jpeg": ".jpg", ".png": ".png", ".gif": ".gif",
                      ".webp": ".webp", ".bmp": ".bmp"}

CONTENT_TYPES = {
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".png": "image/png",
    ".gif": "image/gif",
    ".webp": "image/webp",
    ".bmp": "image/bmp",
}


def root_dir() -> Path:
    path = Path(settings.image_dir)
    path.mkdir(parents=True, exist_ok=True)
    return path


def detect_extension(data: bytes, source_url: str = "") -> str | None:
    """Определяет расширение по содержимому, а не по расширению в ссылке."""
    for magic, ext in _EXT_BY_MAGIC:
        if data.startswith(magic):
            return ext

    if len(data) > 12 and data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        return ".webp"

    suffix = Path(urllib.parse.urlparse(source_url).path).suffix.lower()
    return _EXT_BY_URL_SUFFIX.get(suffix)


def file_path(filename: str) -> Path:
    if not _SAFE_NAME.match(filename):
        raise ValueError("Недопустимое имя файла изображения")
    path = root_dir() / filename
    if path.parent.resolve() != root_dir().resolve():
        raise ValueError("Недопустимое имя файла изображения")
    return path


def new_token() -> str:
    return secrets.token_hex(8)


def save_image(product_id: int, data: bytes, source_url: str = "") -> tuple[str, str] | None:
    """Сохраняет картинку и возвращает (имя файла, токен).

    None, если это не поддерживаемое изображение.
    """
    ext = detect_extension(data, source_url)
    if ext is None or not data:
        return None

    token = new_token()
    filename = f"{product_id}-{token}{ext}"
    file_path(filename).write_bytes(data)
    return filename, token


def read_image(filename: str) -> tuple[Path, str] | None:
    if not filename:
        return None
    try:
        path = file_path(filename)
    except ValueError:
        return None
    if not path.is_file():
        return None
    return path, CONTENT_TYPES.get(path.suffix.lower(), "application/octet-stream")


def delete_image(filename: str) -> None:
    if not filename:
        return
    try:
        file_path(filename).unlink(missing_ok=True)
    except (ValueError, OSError) as exc:
        logger.warning("Не удалось удалить изображение %s: %s", filename, exc)


def delete_all_for(product_id: int) -> None:
    """Удаляет все файлы товара (на случай осиротевших версий)."""
    for path in root_dir().glob(f"{product_id}-*"):
        if _SAFE_NAME.match(path.name):
            try:
                path.unlink()
            except OSError as exc:
                logger.warning("Не удалось удалить %s: %s", path, exc)


def download_remote(url: str) -> bytes:
    """Скачивает картинку по http(s) со ссылки (для импорта товаров)."""
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme not in ("http", "https"):
        raise ValueError("Ссылка на изображение должна начинаться с http:// или https://")

    request = urllib.request.Request(
        url, headers={"User-Agent": "ElectronCash/1.0", "Accept": "image/*,*/*"}
    )
    max_bytes = settings.image_max_bytes

    with urllib.request.urlopen(request, timeout=settings.image_download_timeout) as response:
        chunks: list[bytes] = []
        total = 0
        while True:
            chunk = response.read(64 * 1024)
            if not chunk:
                break
            total += len(chunk)
            if total > max_bytes:
                raise ValueError("Изображение больше допустимого размера")
            chunks.append(chunk)

    return b"".join(chunks)


def cleanup_orphans(known_files: set[str]) -> int:
    """Удаляет файлы, на которые больше не ссылается ни один товар."""
    removed = 0
    for path in root_dir().iterdir():
        if path.is_file() and path.name not in known_files:
            try:
                path.unlink()
                removed += 1
            except OSError:
                pass
    return removed


__all__ = [
    "CONTENT_TYPES",
    "cleanup_orphans",
    "delete_all_for",
    "delete_image",
    "detect_extension",
    "download_remote",
    "file_path",
    "new_token",
    "read_image",
    "root_dir",
    "save_image",
]
