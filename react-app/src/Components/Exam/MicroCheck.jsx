import PopupBase from "../Common/PopupBase";
import React, { useState, useRef } from "react";
import { Mic, MicOff, RotateCcw } from "lucide-react";
import "./MicroCheck.css";

export default function MicroCheck() {
  const [show, setShow] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [audioUrl, setAudioUrl] = useState(null);
  const [recorder, setRecorder] = useState(null);
  const [timer, setTimer] = useState(0);
  const audioRef = useRef(null);

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream);
      const chunks = [];
      mediaRecorder.ondataavailable = (e) => chunks.push(e.data);
      mediaRecorder.onstop = () => {
        const blob = new Blob(chunks, { type: "audio/webm" });
        setAudioUrl(URL.createObjectURL(blob));
      };
      mediaRecorder.start();
      setRecorder(mediaRecorder);
      setIsRecording(true);
      setTimer(0);

      const interval = setInterval(() => {
        setTimer((t) => {
          if (t >= 20) {
            stopRecording();
            clearInterval(interval);
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
    if (recorder) {
      recorder.stop();
      recorder.stream.getTracks().forEach((t) => t.stop());
      setIsRecording(false);
    }
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
        onClose={() => setShow(false)}
      >
        <ul className="instructions-list">
          <li>You have 20 seconds to speak.</li>{" "}
          <li>Please allow microphone access to check.</li>{" "}
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
          <div className="audio-preview">
            <audio ref={audioRef} src={audioUrl} controls />
          </div>
        )}

        {audioUrl && (
          <div className="record-controls">
            <button
              className="record-btn"
              onClick={() => {
                setAudioUrl(null);
                setTimer(0);
              }}
            >
              <RotateCcw size={16} /> Try Again
            </button>
          </div>
        )}
      </PopupBase>
    </>
  );
}
