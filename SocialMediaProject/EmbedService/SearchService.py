from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

app = FastAPI()

# Load the model once when the service starts
model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")


class SearchRequest(BaseModel):
    text: str


@app.post("/embed")
def create_embedding(request: SearchRequest):
    embedding = model.encode(request.text)

    return {
        "embedding": embedding.tolist()
    }