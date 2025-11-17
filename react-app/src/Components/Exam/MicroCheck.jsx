import PopupBase from "../Common/PopupBase";
import React, { useState, useRef, useEffect } from "react";
import { Mic, MicOff, RotateCcw } from "lucide-react";
import "./MicroCheck.css";

function MicroCheck() {
  const [show, setShow] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [audioUrl, setAudioUrl] = useState(null);

  const recorderRef = useRef(null);
  const intervalRef = useRef(null);

  const secondsRef = useRef(20);
  const [seconds, setSeconds] = useState(20);

  useEffect(() => {
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
      try {
        recorderRef.current?.stream?.getTracks()?.forEach((t) => t.stop());
      } catch {}
    };
  }, []);

  useEffect(() => {
    if (show) {
      secondsRef.current = 20;
      setSeconds(20);
      setAudioUrl(null);
      setIsRecording(false);
    }
  }, [show]);

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mr = new MediaRecorder(stream, {
        mimeType: "audio/webm;codecs=opus",
      });

      recorderRef.current = mr;

      const chunks = [];
      mr.ondataavailable = (e) => chunks.push(e.data);
      mr.onstop = () => {
        const blob = new Blob(chunks, { type: "audio/webm" });
        setAudioUrl(URL.createObjectURL(blob));
      };

      mr.start();
      setIsRecording(true);

      secondsRef.current = 20;
      setSeconds(20);

      intervalRef.current = setInterval(() => {
        secondsRef.current -= 1;
        setSeconds(secondsRef.current);

        if (secondsRef.current <= 0) stopRecording();
      }, 1000);
    } catch (err) {
      alert("Cannot access microphone!");
    }
  };

  const stopRecording = () => {
    clearInterval(intervalRef.current);

    try {
      if (recorderRef.current?.state !== "inactive") {
        recorderRef.current.stop();
        recorderRef.current.stream?.getTracks()?.forEach((t) => t.stop());
      }
    } catch {}

    setIsRecording(false);
  };

  const resetAll = () => {
    secondsRef.current = 20;
    setSeconds(20);
    setAudioUrl(null);
    setIsRecording(false);
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

        {/* BUTTONS */}
        <div className="record-controls">
          {!audioUrl && !isRecording && (
            <button className="record-btn" onClick={startRecording}>
              <Mic size={18} /> Check Microphone
            </button>
          )}

          {isRecording && (
            <button className="recording-btn" onClick={stopRecording}>
              <MicOff size={18} /> Stop ({seconds}s)
            </button>
          )}
        </div>

        {audioUrl && (
          <>
            <div className="audio-preview">
              <audio src={audioUrl} controls />
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

export default React.memo(MicroCheck); //  ⬅⬅⬅ FIX QUAN TRỌNG NHẤT
