import { useState, useRef, useCallback, useEffect } from 'react'

/** WebSocket connection states */
type ConnectionState = 'idle' | 'connecting' | 'connected' | 'disconnecting' | 'error'

interface UseVoiceStreamOptions {
  tenantId: string
  profileId: string
  /** Called when audio response bytes arrive from the server */
  onAudioResponse: (audioBytes: ArrayBuffer) => void
  /** Called when the connection state changes */
  onStateChange?: (state: ConnectionState) => void
}

interface UseVoiceStreamReturn {
  connectionState: ConnectionState
  isRecording: boolean
  connect: () => Promise<void>
  disconnect: () => void
  startRecording: () => Promise<void>
  stopRecording: () => void
}

/**
 * React hook managing the full WebRTC/WebSocket voice streaming pipeline.
 *
 * Pipeline:
 *   1. connect()       → opens WebSocket to Gateway (/ws/voice/{tenantId}/{profileId})
 *   2. startRecording() → captures mic audio via getUserMedia, sends chunks via WebSocket
 *   3. onAudioResponse → plays filler/TTS audio received from Orchestrator
 *   4. stopRecording() → stops mic, sends end-of-speech signal
 *   5. disconnect()    → closes WebSocket, releases MediaStream
 */
export function useVoiceStream({
  tenantId,
  profileId,
  onAudioResponse,
  onStateChange,
}: UseVoiceStreamOptions): UseVoiceStreamReturn {
  const [connectionState, setConnectionState] = useState<ConnectionState>('idle')
  const [isRecording, setIsRecording] = useState(false)

  const wsRef = useRef<WebSocket | null>(null)
  const mediaStreamRef = useRef<MediaStream | null>(null)
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)

  const setAndNotifyState = useCallback(
    (state: ConnectionState) => {
      setConnectionState(state)
      onStateChange?.(state)
    },
    [onStateChange]
  )

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      mediaRecorderRef.current?.stop()
      mediaStreamRef.current?.getTracks().forEach(t => t.stop())
      wsRef.current?.close()
    }
  }, [])

  const connect = useCallback(async () => {
    if (wsRef.current?.readyState === WebSocket.OPEN) return

    setAndNotifyState('connecting')

    const wsUrl = `${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/voice/${tenantId}/${profileId}`

    const ws = new WebSocket(wsUrl)
    ws.binaryType = 'arraybuffer'

    ws.onopen = () => setAndNotifyState('connected')

    ws.onmessage = (event: MessageEvent) => {
      if (event.data instanceof ArrayBuffer) {
        onAudioResponse(event.data)
      }
    }

    ws.onerror = () => setAndNotifyState('error')

    ws.onclose = () => {
      setAndNotifyState('idle')
      setIsRecording(false)
    }

    wsRef.current = ws
  }, [tenantId, profileId, onAudioResponse, setAndNotifyState])

  const disconnect = useCallback(() => {
    setAndNotifyState('disconnecting')
    mediaRecorderRef.current?.stop()
    mediaStreamRef.current?.getTracks().forEach(t => t.stop())
    wsRef.current?.close(1000, 'User disconnected')
  }, [setAndNotifyState])

  const startRecording = useCallback(async () => {
    if (wsRef.current?.readyState !== WebSocket.OPEN) {
      throw new Error('WebSocket is not connected. Call connect() first.')
    }

    const stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        channelCount: 1,       // Mono — reduces payload size
        sampleRate: 16000,     // 16 kHz optimal for Whisper ASR
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      },
    })

    mediaStreamRef.current = stream

    const recorder = new MediaRecorder(stream, {
      mimeType: MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
        ? 'audio/webm;codecs=opus'
        : 'audio/webm',
    })

    recorder.ondataavailable = (event: BlobEvent) => {
      if (event.data.size > 0 && wsRef.current?.readyState === WebSocket.OPEN) {
        event.data.arrayBuffer().then(buffer => {
          wsRef.current?.send(buffer)
        })
      }
    }

    // Send 250ms chunks — balances ASR responsiveness vs WebSocket overhead
    recorder.start(250)
    mediaRecorderRef.current = recorder
    setIsRecording(true)
  }, [])

  const stopRecording = useCallback(() => {
    mediaRecorderRef.current?.stop()
    mediaStreamRef.current?.getTracks().forEach(t => t.stop())
    mediaRecorderRef.current = null
    mediaStreamRef.current = null
    setIsRecording(false)
  }, [])

  return {
    connectionState,
    isRecording,
    connect,
    disconnect,
    startRecording,
    stopRecording,
  }
}
