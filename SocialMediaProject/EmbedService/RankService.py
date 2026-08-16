from fastapi import FastAPI
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForCausalLM
import torch
import json
import re

app = FastAPI()

MODEL_NAME = "Qwen/Qwen2.5-1.5B-Instruct"

print("Loading ranking model...")

tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)

model = AutoModelForCausalLM.from_pretrained(
    MODEL_NAME,
    torch_dtype=torch.float32
)

print("Ranking model loaded.")


class Message(BaseModel):
    id: int
    sender: str
    content: str


class Candidate(BaseModel):
    candidateMessageId: int
    messages: list[Message]


class RankRequest(BaseModel):
    query: str
    candidates: list[Candidate]


@app.post("/rank")
def rank_messages(request: RankRequest):

    prompt = f"""
You are a chat search ranking system.

The user searched for:

"{request.query}"

You are given several candidate conversation snippets.

Determine which messages are relevant to the user's search.

Use the surrounding conversation to understand the meaning.

A message does NOT need to contain the exact words from the query.

For each candidate, if ANY message in that candidate group is relevant "
    "to the search query (using the surrounding context to judge meaning), "
    "return the Candidate's ID (not the individual Message ID) with a relevance score.\n"
    "Only return candidates that are truly relevant. Omit irrelevant candidates entirely.\n\n"

For example:

User: When are you leaving to Hyderabad?
Anitha: Tomorrow morning

Search:
When is Anitha leaving to Hyderabad?

The message "Tomorrow morning" is highly relevant because it
answers the user's question.

Ignore unrelated conversations which does not relate to the search query.

Return ONLY JSON.

The format must be:

[
  {{
    "messageId": 123,
    "score": 0.95
  }}
]

CANDIDATES:
"""
    

    for candidate in request.candidates:

        prompt += f"\nCandidate {candidate.candidateMessageId}:\n"

        for message in candidate.messages:

            prompt += (
                f"Message ID: {message.id}\n"
                f"Sender: {message.sender}\n"
                f"Content: {message.content}\n"
            )

        prompt += "\n---\n"

    messages = [
        {
            "role": "user",
            "content": prompt
        }
    ]

    text = tokenizer.apply_chat_template(
        messages,
        tokenize=False,
        add_generation_prompt=True
    )

    inputs = tokenizer(
        text,
        return_tensors="pt"
    )

    with torch.no_grad():

        outputs = model.generate(
            **inputs,
            max_new_tokens=300,
            temperature=0.1,
            do_sample=False
        )

    generated_tokens = outputs[0][inputs["input_ids"].shape[1]:]

    result_text = tokenizer.decode(
        generated_tokens,
        skip_special_tokens=True
    )

    print("LLM response:")
    print(result_text)

    # Extract JSON array
    match = re.search(
        r"\[[\s\S]*\]",
        result_text
    )

    if not match:
        return {
            "results": []
        }

    ranked_results = json.loads(match.group())

    return {
        "results": ranked_results
    }