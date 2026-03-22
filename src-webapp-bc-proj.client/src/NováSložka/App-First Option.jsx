import { useState, useEffect, useRef } from 'react';
import './App.css';

function App() {
  const [isRecording, setIsRecording] = useState(false);
  const [note, setNote] = useState('');
  const [notesStore, setNotesStore] = useState([]);

  // Uchovává instanci mikrofonu napříč rendery
  const micRef = useRef(null);

  useEffect(() => {
    const SpeechRecognition =
      window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
      alert("Your browser doesn't support Speech Recognition.");
      return;
    }

    const microphone = new SpeechRecognition();
    microphone.continuous = true;
    microphone.interimResults = true;
    microphone.lang = 'en-US';
    micRef.current = microphone;

    microphone.onresult = (event) => {
      const recordingResult = Array.from(event.results)
        .map((result) => result[0])
        .map((result) => result.transcript)
        .join('');
      setNote(recordingResult);
    };

    microphone.onerror = (event) => {
      console.error('Microphone error:', event.error);
    };

    return () => {
      microphone.stop();
    };
  }, []);

  useEffect(() => {
    const microphone = micRef.current;
    if (!microphone) return;

    if (isRecording) {
      console.log('🎙️ Starting recording...');
      microphone.start();
    } else {
      console.log('🛑 Stopping recording...');
      microphone.stop();
    }

    microphone.onend = () => {
      if (isRecording) {
        console.log('Continuing recording...');
        microphone.start();
      } else {
        console.log('Microphone stopped.');
      }
    };
  }, [isRecording]);

  const storeNote = () => {
    if (note.trim() !== '') {
      setNotesStore((prev) => [...prev, note]);
      setNote('');
    }
  };

  return (
    <div className="App">
      <h1>🎤 Record Voice Notes</h1>
      <div className="noteContainer">
        <h2>Record Note Here</h2>
        <p>Status: {isRecording ? 'Recording...' : 'Stopped'}</p>

        <div className="controls">
          <button className="button" onClick={storeNote} disabled={!note}>
            Save
          </button>
          <button onClick={() => setIsRecording((prev) => !prev)}>
            {isRecording ? 'Stop' : 'Start'}
          </button>
        </div>

        <p className="currentNote">{note}</p>
      </div>

      <div className="noteContainer">
        <h2>Notes Store</h2>
        {notesStore.length === 0 ? (
          <p>No notes yet.</p>
        ) : (
          notesStore.map((n, i) => <p key={i}>{n}</p>)
        )}
      </div>
    </div>
  );
}

export default App;
