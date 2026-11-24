import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from . import images as image_store
from .database import Base, SessionLocal, engine, wait_for_db
from .migrations import run_migrations
from .routers import admin, auth, catalog, orders, stats
from .routers import images as images_router
from .seed import seed_database

logging.basicConfig(level=logging.INFO)


@asynccontextmanager
async def lifespan(_: FastAPI):
    wait_for_db()
    run_migrations(engine)
    Base.metadata.create_all(bind=engine)
    image_store.root_dir()
    with SessionLocal() as db:
        seed_database(db)
    yield


app = FastAPI(title="ElectronCash API", version="1.0.0", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auth.router)
app.include_router(catalog.router)
app.include_router(images_router.router)
app.include_router(orders.router)
app.include_router(stats.router)
app.include_router(admin.router)


@app.get("/health", tags=["system"])
def health() -> dict[str, str]:
    return {"status": "ok"}
