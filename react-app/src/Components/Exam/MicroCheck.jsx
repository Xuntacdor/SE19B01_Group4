import PopupBase from "../Common/PopupBase";
import React, { useState, useRef, useEffect } from "react";
import { Mic, MicOff, RotateCcw } from "lucide-react";
import "./MicroCheck.css";

export default function MicroCheck() {
  const [show, setShow] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [audioUrl, setAudioUrl] = useState(null);
  const [recorder, setRecorder] = useState(null);
  const [timer, setTimer] = useState(0);
  const intervalRef = useRef(null);
  const audioRef = useRef(null);

  useEffect(() => {
    return () => {
      // cleanup khi unmount
      if (intervalRef.current) clearInterval(intervalRef.current);
      if (recorder) {
        try {
          recorder.stream?.getTracks()?.forEach((t) => t.stop());
        } catch {}
      }
    };
  }, [recorder]);

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      // chọn mimeType phổ biến nhất
      const mr = new MediaRecorder(stream, {
        mimeType: "audio/webm;codecs=opus",
      });
      const chunks = [];
      mr.ondataavailable = (e) => chunks.push(e.data);
      mr.onstop = () => {
        const blob = new Blob(chunks, { type: "audio/webm" });
        setAudioUrl(URL.createObjectURL(blob));
      };
      mr.start();
      setRecorder(mr);
      setIsRecording(true);
      setTimer(0);

      // đếm 20s, tự dừng
      intervalRef.current = setInterval(() => {
        setTimer((t) => {
          if (t >= 19) {
            stopRecording();
            return 20;
          }
          return t + 1;
        });
      }, 1000);
    } catch (err) {
      alert("Cannot access microphone!");
      console.error(err);
    }
  };

  const stopRecording = () => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    if (recorder && recorder.state !== "inactive") {
      recorder.stop();
      recorder.stream.getTracks().forEach((t) => t.stop());
    }
    setIsRecording(false);
  };

  const resetAll = () => {
    setAudioUrl(null);
    setTimer(0);
  };

  return (
    <>
      <button className="microcheck-btn" onClick={() => setShow(true)}>
        <Mic size={18} /> Micro Check
      </button>

      <PopupBase
        title=" Microphone Check"
        icon={Mic}
        show={show}
        width="420px"
        onClose={() => {
          setShow(false);
          stopRecording();
        }}
      >
        <ul className="instructions-list">
          <li>You have 20 seconds to speak.</li>
          <li>Please allow microphone access to check.</li>
        </ul>

        <div className="record-controls">
          {!audioUrl && !isRecording && (
            <button className="record-btn" onClick={startRecording}>
              <Mic size={18} /> Check Microphone
            </button>
          )}

          {isRecording && (
            <button className="recording-btn" onClick={stopRecording}>
              <MicOff size={18} /> Stop ({timer}s)
            </button>
          )}
        </div>

        {audioUrl && (
          <>
            <div className="audio-preview">
              <audio ref={audioRef} src={audioUrl} controls />
            </div>
            <div className="record-controls">
              <button className="record-btn" onClick={resetAll}>
                <RotateCcw size={16} /> Try Again
              </button>
            </div>
          </>
        )}
      </PopupBase>
    </>
  );
}
