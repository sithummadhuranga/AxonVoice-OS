# Ollama Setup Guide — AxonVoice OS

> **Architectural Decision:** Ollama runs **natively** on the host machine, NOT in Docker.
> See `docs/changelog/ARCH-001-initial-mono-repo-scaffold.md` § ADR-001-D for rationale.

---

## 1. Native Installation (Recommended for All Environments)

### Windows
```powershell
# Download and run the installer from https://ollama.com/download/windows
# Or via winget:
winget install Ollama.Ollama
```

### macOS
```bash
brew install ollama
```

### Linux (Ubuntu/Debian)
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

---

## 2. Start Ollama Service

### Windows (runs as a background service after install)
```powershell
ollama serve   # starts on http://localhost:11434
```

### macOS / Linux
```bash
ollama serve &
```

---

## 3. Pull Required Models

```bash
# Primary LLM (English reasoning + function calling)
ollama pull llama3:8b

# Sinhala fine-tuned model (bilingual routing)
ollama pull qwen2.5:1.5b

# Whisper ASR (audio transcription + language detection)
ollama pull whisper
```

---

## 4. Configure Environment Variable

In your `.env` file (copied from `.env.example`):

```env
# Docker services reach native Ollama via this address
OLLAMA_BASE_URL=http://host.docker.internal:11434

# For local development (non-Docker)
# OLLAMA_BASE_URL=http://localhost:11434
```

> **Note:** `host.docker.internal` resolves to your host machine's IP from inside Docker containers on Windows/macOS. On Linux, add `--add-host=host.docker.internal:host-gateway` to your Docker run command, or use your actual host IP.

---

## 5. Verify Ollama is Running

```bash
curl http://localhost:11434/api/tags
# Expected: JSON list of downloaded models
```

---

## 6. Optional: Docker (Confirmed WSL2 GPU Passthrough Only)

Only use this if you have verified that Docker Desktop can access your NVIDIA GPU via WSL2:

```bash
# Start only the Ollama container (opt-in profile)
docker-compose --profile gpu-docker up ollama-engine

# Verify GPU access inside container
docker exec axon_ollama nvidia-smi
```

**Prerequisites for Docker GPU mode:**
- NVIDIA driver ≥ 535 on Windows host
- WSL2 kernel ≥ 5.10.43.3
- NVIDIA Container Toolkit installed in WSL2
- Docker Desktop ≥ 4.25 with GPU enabled in Settings → Resources

---

## 7. GPU Memory Requirements

| Model | Min VRAM |
|---|---|
| `llama3:8b` (Q4) | 5 GB |
| `qwen2.5:1.5b` | 2 GB |
| `whisper` (medium) | 2.5 GB |
| **Total recommended** | **≥ 8 GB VRAM** |

For 4–6 GB VRAM cards: Use `llama3:8b` with Q4 quantization only and unload models between requests.
