import io
import os
from contextlib import asynccontextmanager
from pathlib import Path

import uvicorn
from fastapi import FastAPI, HTTPException, Request
from fastapi.concurrency import run_in_threadpool
from PIL import Image, UnidentifiedImageError
from pydantic import BaseModel
from transformers import pipeline

MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024


class Prediction(BaseModel):
    label: str
    score: float


@asynccontextmanager
async def lifespan(app: FastAPI):
    model_path = os.environ.get("FOOD_MODEL_PATH")
    if not model_path or not Path(model_path).is_dir():
        raise RuntimeError("Set FOOD_MODEL_PATH to the fine-tuned model directory.")

    app.state.classifier = pipeline("image-classification", model=model_path)
    yield


app = FastAPI(title="Diyet food model", lifespan=lifespan)


@app.get("/health")
async def health() -> dict[str, str]:
    if not hasattr(app.state, "classifier"):
        raise HTTPException(status_code=503, detail="The model is not loaded.")
    return {"status": "ready"}


@app.post("/predict", response_model=Prediction)
async def predict(request: Request) -> Prediction:
    image_bytes = await request.body()
    if not image_bytes or len(image_bytes) > MAX_IMAGE_SIZE_BYTES:
        raise HTTPException(status_code=400, detail="The image is empty or exceeds 5 MB.")

    try:
        with Image.open(io.BytesIO(image_bytes)) as source:
            image = source.convert("RGB")
    except (UnidentifiedImageError, OSError) as exception:
        raise HTTPException(status_code=400, detail="The image could not be decoded.") from exception

    predictions = await run_in_threadpool(app.state.classifier, image, top_k=1)
    if not predictions:
        raise HTTPException(status_code=502, detail="The model returned no predictions.")

    result = predictions[0]
    return Prediction(label=str(result["label"]), score=float(result["score"]))


if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8001)
