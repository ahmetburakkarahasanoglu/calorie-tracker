# Local Turkish food model

This service loads the fine-tuned ViT image-classification model once and serves
predictions to the .NET API on `127.0.0.1:8001`. Keep the model weights outside
source control and point `FOOD_MODEL_PATH` at the directory containing
`config.json` and `model.safetensors`.

From the repository root, activate the Python environment used to fine-tune the
model and install the service dependencies, then start it:

```bash
source "$HOME/Dıyet_Model/venv/bin/activate"
python -m pip install -r model-service/requirements.txt
export FOOD_MODEL_PATH="$HOME/Dıyet_Model/turk_yemek_modeli_102"
python model-service/app.py
```

On Windows, activate that environment with
`%USERPROFILE%\Dıyet_Model\venv\Scripts\activate` and set `FOOD_MODEL_PATH` to
the corresponding model directory before running `python model-service/app.py`.

Keep this process running while using the app. Check that it is ready with
`curl http://127.0.0.1:8001/health`. The service intentionally binds to
localhost; do not expose it directly to the public internet.
