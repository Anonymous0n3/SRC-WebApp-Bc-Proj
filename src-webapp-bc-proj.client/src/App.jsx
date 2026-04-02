import React, { useRef, useState, createContext, useContext } from "react";
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from "react-router-dom";
import "./App.css";
import recStyles from "./Recorder.module.css";
import { useLocation } from "react-router-dom";
import ReactGA from "react-ga4";

// ============================================================================
// 0. THEMES AND CONTEXT FOR DARK/LIGHT MODE
// ============================================================================

const themes = {
    dark: {
        appBg: '#0f0f0f',
        navBg: '#111',
        cardBg: '#222',
        statusBg: '#000',
        controlBg: '#1a1a1a',
        textMain: '#fff',
        textSec: '#aaa',
        textMuted: '#888',
        borderMain: '#333',
        borderSec: '#444',
        btnBg: '#333',
        btnBgHover: '#444'
    },
    light: {
        appBg: '#f5f5f5',
        navBg: '#ffffff',
        cardBg: '#ffffff',
        statusBg: '#e9ecef',
        controlBg: '#f8f9fa',
        textMain: '#212529',
        textSec: '#495057',
        textMuted: '#6c757d',
        borderMain: '#dee2e6',
        borderSec: '#ced4da',
        btnBg: '#e0e0e0',
        btnBgHover: '#d5d5d5'
    }
};

const ThemeContext = createContext();

// ============================================================================
// 1. SHARED DATA AND RECORDING LOGIC (Custom Hook)
// ============================================================================

const getUserId = () => {
    let uid = sessionStorage.getItem('gmdss_user_id');
    if (!uid) {
        uid = 'user_' + Math.random().toString(36).substr(2, 9);
        sessionStorage.setItem('gmdss_user_id', uid);
    }
    return uid;
};

const useRecorder = (emergencyType) => {
    const mediaRecorderRef = useRef(null);
    const [recordingStatus, setRecordingStatus] = useState("idle");
    const [recordingTime, setRecordingTime] = useState(0);
    const [intervalId, setIntervalId] = useState(null);
    const [audioBlob, setAudioBlob] = useState(null);
    const chunksRef = useRef([]);

    const [systemMessage, setSystemMessage] = useState("SYSTEM READY. STANDBY.");
    const [validationResult, setValidationResult] = useState(null);

    const vesselData = { name: "OCEAN EXPLORER", callSign: "OL2491", mmsi: "123456789", pob: 12 };
    const gps = { lat: 49.82345, lon: 15.12345, speed: 12.5, heading: 87 };

    const incidentData = {
        MAYDAY: { problem: "BOAT IS SINKING" },
        PAN_PAN: { problem: "ENGINE FAILURE", assistance: "REQUIRE TOW TO NEAREST PORT" },
        SECURITE: { problem: "LARGE FLOATING DEBRIS IN CHANNEL", assistance: "N/A" },
        RADIO_CHECK_SHIP: { problem: "UNKNOWN VESSEL", assistance: "HOW DO YOU READ ME" },
        RADIO_CHECK_STATION: { problem: "SPLIT RADIO", assistance: "HOW DO YOU READ ME" }
    };

    const currentIncident = incidentData[emergencyType] || { problem: "", assistance: "" };
    const userId = getUserId();

    const startTimer = () => {
        const id = setInterval(() => setRecordingTime((t) => t + 1), 1000);
        setIntervalId(id);
    };

    const stopTimer = () => {
        clearInterval(intervalId);
        setIntervalId(null);
    };

    const startRecording = async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            setValidationResult(null);
            setSystemMessage(`🔴 TX ACTIVE: ${emergencyType.replace(/_/g, ' ')} BROADCAST`);

            mediaRecorderRef.current = new MediaRecorder(stream);
            chunksRef.current = [];

            mediaRecorderRef.current.ondataavailable = (e) => {
                if (e.data.size > 0) chunksRef.current.push(e.data);
            };

            mediaRecorderRef.current.onstop = () => {
                const blob = new Blob(chunksRef.current, { type: "audio/webm" });
                setAudioBlob(blob);
                setSystemMessage("⏳ UPLOADING DATA TO BACKEND...");
                sendToBackend(blob);
            };

            mediaRecorderRef.current.start();
            setRecordingStatus("recording");
            setRecordingTime(0);
            startTimer();
        } catch (err) {
            console.error("Microphone access error:", err);
            alert("Unable to start recording. Please check microphone permissions.");
        }
    };

    const stopRecording = () => {
        if (mediaRecorderRef.current && mediaRecorderRef.current.state !== "inactive") {
            mediaRecorderRef.current.stop();
            mediaRecorderRef.current.stream.getTracks().forEach((t) => t.stop());
            setRecordingStatus("stopped");
            stopTimer();
        }
    };

    const sendToBackend = async (blob) => {
        const terminalData = {
            vesselName: vesselData.name, callSign: vesselData.callSign, mmsi: vesselData.mmsi, pob: vesselData.pob,
            latitude: gps.lat.toFixed(5), longitude: gps.lon.toFixed(5), speed: gps.speed, heading: gps.heading,
            natureOfDistress: currentIncident.problem, assistanceRequired: currentIncident.assistance || "", timestamp: new Date().toISOString()
        };

        const formData = new FormData();
        formData.append("Audio", blob, "recording.webm");
        formData.append("TerminalData", JSON.stringify(terminalData));
        formData.append("EmergencyType", emergencyType);
        formData.append("UserId", userId);

        try {
            const response = await fetch('/api/emergency-upload', { method: 'POST', body: formData });
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            const result = await response.json();
            setSystemMessage(result.message);
            setValidationResult({ isValid: result.isValid, expected: result.expectedText, actual: result.actualText });
        } catch (error) {
            console.error("Error communicating with backend:", error);
            setSystemMessage("❌ TX FAILED: CONNECTION ERROR");
            setValidationResult(null);
        }
    };

    return { recordingStatus, recordingTime, audioBlob, systemMessage, validationResult, vesselData, gps, currentIncident, startRecording, stopRecording };
};

// ============================================================================
// 2. USER MANUAL PAGE
// ============================================================================

const ManualPage = () => {
    const { isDark } = useContext(ThemeContext);
    const theme = isDark ? themes.dark : themes.light;
    const navigate = useNavigate();

    return (
        <div className={recStyles.appContainer} style={{ backgroundColor: theme.appBg, minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '40px 20px' }}>
            <div style={{ maxWidth: '800px', backgroundColor: theme.cardBg, padding: '40px', borderRadius: '12px', boxShadow: isDark ? 'none' : '0 10px 25px rgba(0,0,0,0.1)', border: `1px solid ${theme.borderSec}` }}>
                <h1 style={{ color: isDark ? '#00ffcc' : '#008066', textAlign: 'center', marginBottom: '30px', fontSize: '2.5rem' }}>GMDSS Simulator Manual</h1>

                <div style={{ color: theme.textMain, fontSize: '1.1rem', lineHeight: '1.6' }}>
                    <h3 style={{ color: theme.textSec, borderBottom: `1px solid ${theme.borderMain}`, paddingBottom: '10px' }}>1. Welcome</h3>
                    <p>This simulator is designed to help you practice proper GMDSS communication protocols, including Distress, Urgency, Safety, and Routine calls.</p>

                    <h3 style={{ color: theme.textSec, borderBottom: `1px solid ${theme.borderMain}`, paddingBottom: '10px', marginTop: '30px' }}>2. How to use the simulator</h3>
                    <ul style={{ paddingLeft: '20px' }}>
                        <li style={{ marginBottom: '10px' }}><strong>Select a scenario:</strong> From the main menu, click on the type of call you want to practice.</li>
                        <li style={{ marginBottom: '10px' }}><strong>Review terminal data:</strong> Each scenario provides vessel identification and GPS position data necessary for your call.</li>
                        <li style={{ marginBottom: '10px' }}><strong>Transmit:</strong> Click the <span style={{ backgroundColor: theme.btnBg, padding: '2px 6px', borderRadius: '4px' }}>TRANSMIT</span> button to open your microphone.</li>
                        <li style={{ marginBottom: '10px' }}><strong>Speech Protocol:</strong> You must speak clearly and include both the <strong>CALL</strong> (initial identification) and the <strong>MESSAGE</strong> (vessel details and position) in one transmission.</li>
                        <li style={{ marginBottom: '10px' }}><strong>End transmission:</strong> Click <span style={{ backgroundColor: theme.btnBg, padding: '2px 6px', borderRadius: '4px' }}>END TX</span> when you finish speaking.</li>
                    </ul>

                    <h3 style={{ color: '#ff4444', borderBottom: `1px solid ${theme.borderMain}`, paddingBottom: '10px', marginTop: '30px' }}>3. Requirements & Limitations</h3>
                    <ul style={{ paddingLeft: '20px' }}>
                        <li style={{ marginBottom: '10px' }}><strong>Microphone:</strong> You must allow microphone access in your browser.</li>
                        <li style={{ marginBottom: '10px' }}><strong>Language:</strong> The voice recognition system currently supports <strong>ENGLISH ONLY</strong>. Please perform all transmissions in English.</li>
                    </ul>
                </div>

                <div style={{ textAlign: 'center', marginTop: '40px' }}>
                    <button
                        onClick={() => navigate("/menu")}
                        style={{ backgroundColor: isDark ? '#00ffcc' : '#008066', color: isDark ? '#000' : '#fff', padding: '15px 40px', fontSize: '1.2rem', fontWeight: 'bold', border: 'none', borderRadius: '8px', cursor: 'pointer', transition: '0.2s' }}
                    >
                        START SIMULATOR
                    </button>
                </div>
            </div>
        </div>
    );
};

// ============================================================================
// 3. UNIVERSAL CALL SCREEN (Kód zůstává beze změn)
// ============================================================================

const CallScreen = ({ type, color, title, showPob = false, showSpeedHeading = false, showIncidentDetails = false }) => {
    const { isDark } = useContext(ThemeContext);
    const theme = isDark ? themes.dark : themes.light;

    const {
        recordingStatus, recordingTime, audioBlob,
        systemMessage, validationResult, vesselData, gps, currentIncident, startRecording, stopRecording
    } = useRecorder(type);

    const navigate = useNavigate();

    const getMessageColor = () => {
        if (validationResult) return validationResult.isValid ? (isDark ? '#00ffcc' : '#00b38f') : '#ff4444';
        if (systemMessage.includes('READY')) return isDark ? '#00ffcc' : '#00b38f';
        if (systemMessage.includes('FAILED')) return '#ff4444';
        return isDark ? '#ffcc00' : '#e6b800';
    };

    const getIncidentTitle = () => {
        if (type === 'MAYDAY') return "NATURE OF DISTRESS";
        if (type === 'PAN_PAN') return "URGENCY CONDITION";
        if (type === 'RADIO_CHECK_SHIP') return "TARGET VESSEL";
        if (type === 'RADIO_CHECK_STATION') return "TARGET STATION";
        return "SAFETY WARNING";
    };

    return (
        <div className={recStyles.appContainer} style={{ backgroundColor: theme.appBg, color: theme.textMain, minHeight: '100vh', paddingBottom: '40px' }}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: '20px', paddingTop: '20px' }}>
                <h1 className={recStyles.title} style={{ color: color, marginBottom: '15px' }}>{title} TERMINAL</h1>
                <button onClick={() => navigate("/menu")} style={{ padding: '10px 20px', backgroundColor: theme.btnBg, color: theme.textMain, border: 'none', borderRadius: '5px', cursor: 'pointer', fontWeight: 'bold' }}>
                    ← Back to Menu
                </button>
            </div>

            <div className={recStyles.mainContent} style={{ display: 'flex', gap: '20px', flexWrap: 'wrap', padding: '0 20px' }}>
                <div style={{ flex: '1', minWidth: '350px' }}>
                    <div className={recStyles.navTerminal} style={{ backgroundColor: theme.navBg, border: `2px solid ${color}`, padding: '15px', borderRadius: '8px' }}>
                        <h3 className={recStyles.terminalTitle} style={{ borderBottom: `1px solid ${theme.borderSec}`, paddingBottom: '10px', color: color }}>VESSEL IDENTIFICATION</h3>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', color: theme.textMain, fontSize: '1.1rem' }}>
                            <div><span style={{ color: theme.textMuted }}>NAME:</span> <br /><b>{vesselData.name}</b></div>
                            <div><span style={{ color: theme.textMuted }}>CALL SIGN:</span> <br /><b>{vesselData.callSign}</b></div>
                            <div><span style={{ color: theme.textMuted }}>MMSI:</span> <br /><b>{vesselData.mmsi}</b></div>
                            {showPob && <div><span style={{ color: theme.textMuted }}>PERSONS ON BOARD:</span> <br /><b>{vesselData.pob}</b></div>}
                        </div>

                        <h3 className={recStyles.terminalTitle} style={{ borderBottom: `1px solid ${theme.borderSec}`, paddingBottom: '10px', marginTop: '20px', color: color }}>
                            {type === 'SECURITE' ? 'HAZARD POSITION (UTC)' : 'POSITION DATA (UTC)'}
                        </h3>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', color: isDark ? '#00ffcc' : '#008066', fontSize: '1.2rem', fontFamily: 'monospace' }}>
                            <div><span style={{ color: theme.textMuted, fontSize: '0.9rem' }}>LAT:</span> <br /><b>{gps.lat.toFixed(5)} N</b></div>
                            <div><span style={{ color: theme.textMuted, fontSize: '0.9rem' }}>LON:</span> <br /><b>{gps.lon.toFixed(5)} E</b></div>
                            {showSpeedHeading && (
                                <>
                                    <div><span style={{ color: theme.textMuted, fontSize: '0.9rem' }}>COG:</span> <br /><b>{gps.heading}°</b></div>
                                    <div><span style={{ color: theme.textMuted, fontSize: '0.9rem' }}>SOG:</span> <br /><b>{gps.speed} kt</b></div>
                                </>
                            )}
                        </div>

                        {showIncidentDetails && (
                            <>
                                <h3 className={recStyles.terminalTitle} style={{ borderBottom: `1px solid ${theme.borderSec}`, paddingBottom: '10px', marginTop: '20px', color: color }}>
                                    {type === 'SECURITE' ? 'SAFETY BROADCAST DETAILS' : 'DETAILS'}
                                </h3>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', color: theme.textMain, fontSize: '1.1rem' }}>
                                    <div><span style={{ color: theme.textMuted }}>{getIncidentTitle()}:</span> <br /><b>{currentIncident.problem}</b></div>

                                    {type !== 'SECURITE' && currentIncident.assistance && (
                                        <div><span style={{ color: theme.textMuted }}>{type.startsWith('RADIO_CHECK') ? 'REQUEST:' : 'ASSISTANCE REQUIRED:'}</span> <br /><b>{currentIncident.assistance}</b></div>
                                    )}
                                </div>
                            </>
                        )}
                    </div>

                    <div style={{ marginTop: '15px', padding: '15px', backgroundColor: theme.statusBg, border: `1px solid ${theme.borderSec}`, borderRadius: '8px' }}>
                        <p style={{ margin: '0 0 5px 0', color: theme.textMuted, fontSize: '0.8rem', textTransform: 'uppercase' }}>System Status</p>
                        <p style={{ margin: 0, color: getMessageColor(), fontWeight: 'bold' }}>{systemMessage}</p>

                        {validationResult && (
                            <div style={{ marginTop: '15px', paddingTop: '10px', borderTop: `1px solid ${theme.borderMain}`, fontSize: '0.9rem' }}>
                                <div style={{ marginBottom: '10px' }}>
                                    <span style={{ color: theme.textMuted, textTransform: 'uppercase' }}>Expected Format:</span><br />
                                    <span style={{ color: theme.textSec, fontStyle: 'italic' }}>"{validationResult.expected}"</span>
                                </div>
                                <div>
                                    <span style={{ color: theme.textMuted, textTransform: 'uppercase' }}>
                                        {type.startsWith('RADIO_CHECK') ? 'RECEIVED RESPONSE:' : 'TRANSMITTED AUDIO (STT):'}
                                    </span><br />
                                    <span style={{ color: validationResult.isValid ? (isDark ? '#00ffcc' : '#00b38f') : (isDark ? '#ffaa00' : '#cc8800'), fontStyle: 'italic' }}>
                                        "{validationResult.actual}"
                                    </span>
                                </div>
                            </div>
                        )}
                    </div>
                </div>

                <div className={recStyles.audioRecorder} style={{ flex: '1', minWidth: '300px', backgroundColor: theme.controlBg, border: `1px solid ${color}`, borderRadius: '8px', padding: '20px' }}>
                    <div style={{ textAlign: 'center', marginBottom: '20px' }}>
                        <h3 style={{ color: theme.textMain, marginTop: 0 }}>TX CONTROLS</h3>
                        <p style={{ color: recordingStatus === "recording" ? '#ff4444' : theme.textMuted, fontWeight: 'bold', fontSize: '1.2rem' }}>
                            {recordingStatus === "recording" ? `MIC LIVE — ${recordingTime}s` : "MIC OFF"}
                        </p>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                        <button
                            style={{ backgroundColor: recordingStatus === "recording" ? theme.btnBgHover : color, color: 'white', fontWeight: 'bold', padding: '20px', fontSize: '1.2rem', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                            onClick={startRecording} disabled={recordingStatus === "recording"}
                        >
                            TRANSMIT {type.replace(/_/g, ' ')}
                        </button>
                    </div>

                    <div style={{ borderTop: `2px solid ${theme.borderMain}`, margin: '20px 0' }}></div>

                    <div style={{ display: 'flex', gap: '10px' }}>
                        <button style={{ flex: 1, padding: '15px', backgroundColor: recordingStatus === "recording" ? theme.textMain : theme.btnBg, color: recordingStatus === "recording" ? theme.appBg : theme.textMuted, border: 'none', fontWeight: 'bold', cursor: 'pointer', borderRadius: '4px' }} onClick={stopRecording} disabled={recordingStatus !== "recording"}>
                            END TX
                        </button>
                    </div>

                    {audioBlob && (
                        <div style={{ marginTop: '20px' }}>
                            <audio style={{ width: "100%" }} controls src={URL.createObjectURL(audioBlob)} />
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

// ============================================================================
// 4. MAIN MENU
// ============================================================================

const MainMenu = () => {
    const { isDark } = useContext(ThemeContext);
    const theme = isDark ? themes.dark : themes.light;

    return (
        <div className={recStyles.appContainer} style={{ textAlign: 'center', paddingTop: '50px', backgroundColor: theme.appBg, minHeight: '100vh' }}>
            <h1 className={recStyles.title} style={{ color: theme.textMain }}>GMDSS TRAINING SIMULATOR</h1>
            <p style={{ color: theme.textSec, marginBottom: '40px' }}>Select communication type to practice:</p>

            <div style={{ display: 'flex', justifyContent: 'center', gap: '20px', flexWrap: 'wrap', padding: '0 20px' }}>
                {[
                    { path: "/mayday", color: "#cc0000", title: "MAYDAY", sub: "Distress Communication" },
                    { path: "/panpan", color: "#d48800", title: "PAN PAN", sub: "Urgency Communication" },
                    { path: "/securite", color: "#0066cc", title: "SECURITE", sub: "Safety Communication" },
                    { path: "/radiocheck-ship", color: "#009933", title: "RADIO CHECK", sub: "Ship to Ship" },
                    { path: "/radiocheck-station", color: "#009933", title: "RADIO CHECK", sub: "Ship to Station" }
                ].map((item) => (
                    <Link key={item.path} to={item.path} style={{ textDecoration: 'none' }}>
                        <div style={{ backgroundColor: theme.cardBg, border: `2px solid ${item.color}`, padding: '40px', borderRadius: '10px', width: '250px', cursor: 'pointer', transition: '0.3s', boxShadow: isDark ? 'none' : '0 4px 6px rgba(0,0,0,0.1)' }}>
                            <h2 style={{ color: item.color, margin: '0 0 10px 0' }}>{item.title}</h2>
                            <p style={{ color: theme.textMuted, margin: 0 }}>{item.sub}</p>
                        </div>
                    </Link>
                ))}
            </div>
        </div>
    );
};

const CreditsPage = () => {
    const { isDark } = useContext(ThemeContext);
    const theme = isDark ? themes.dark : themes.light;
    return (
        <div className={recStyles.appContainer} style={{ backgroundColor: theme.appBg, minHeight: '100vh', padding: '20px' }}>
            <h1 className={recStyles.title} style={{ color: theme.textMain }}>Credits</h1>
            <p style={{ color: theme.textSec }}><strong>VOSK</strong>: This project incorporates the Vosk Offline Speech Recognition Toolkit, which is licensed under the Apache License, Version 2.0.</p>
            <Link to="/menu" style={{ color: isDark ? '#00ffcc' : '#008066', fontWeight: 'bold' }}>← Back to Menu</Link>
        </div>
    );
};

// ============================================================================
// 5. GOOGLE ANALYTICS
// ============================================================================
// Inicializace Google Analytics
ReactGA.initialize("G-948DERKD08");

// Komponenta, která sleduje, na jaké stránce uživatel zrovna je
const PageTracker = () => {
    const location = useLocation();

    React.useEffect(() => {
        // Pošle informaci o zobrazení stránky do GA pokaždé, když se změní URL
        ReactGA.send({ hitType: "pageview", page: location.pathname + location.search });
    }, [location]);

    return null; // Tato komponenta nic nevykresluje, běží jen na pozadí
};

// ============================================================================
// 6. ROUTER AND THEME PROVIDER
// ============================================================================

export default function App() {
    const [isDark, setIsDark] = useState(true);

    const toggleTheme = () => setIsDark(!isDark);
    const theme = isDark ? themes.dark : themes.light;

    return (
        <ThemeContext.Provider value={{ isDark, toggleTheme }}>
            <Router>
                <PageTracker />
                <nav style={{ padding: "10px 20px", background: theme.navBg, borderBottom: `1px solid ${theme.borderMain}`, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                        <Link to="/" style={{ color: theme.textSec, marginRight: "20px", textDecoration: 'none', fontWeight: 'bold' }}>MANUAL</Link>
                        <Link to="/menu" style={{ color: theme.textSec, marginRight: "20px", textDecoration: 'none', fontWeight: 'bold' }}>MENU</Link>
                        <Link to="/credits" style={{ color: theme.textSec, textDecoration: 'none', fontWeight: 'bold' }}>CREDITS</Link>
                    </div>
                    <button
                        onClick={toggleTheme}
                        style={{ background: 'transparent', border: `1px solid ${theme.borderSec}`, color: theme.textMain, padding: '5px 10px', borderRadius: '5px', cursor: 'pointer', fontSize: '1rem' }}
                        title="Toggle Light/Dark Mode"
                    >
                        {isDark ? "☀️ Light" : "🌙 Dark"}
                    </button>
                </nav>

                <Routes>
                    <Route path="/" element={<ManualPage />} />
                    <Route path="/menu" element={<MainMenu />} />
                    <Route path="/credits" element={<CreditsPage />} />
                    <Route path="/mayday" element={<CallScreen type="MAYDAY" color="#cc0000" title="DISTRESS (MAYDAY)" showPob={true} showSpeedHeading={true} showIncidentDetails={true} />} />
                    <Route path="/panpan" element={<CallScreen type="PAN_PAN" color="#d48800" title="URGENCY (PAN PAN)" showPob={false} showSpeedHeading={false} showIncidentDetails={true} />} />
                    <Route path="/securite" element={<CallScreen type="SECURITE" color="#0066cc" title="SAFETY (SECURITE)" showPob={false} showSpeedHeading={false} showIncidentDetails={true} />} />
                    <Route path="/radiocheck-ship" element={<CallScreen type="RADIO_CHECK_SHIP" color="#009933" title="ROUTINE (SHIP TO SHIP)" showPob={false} showSpeedHeading={false} showIncidentDetails={true} />} />
                    <Route path="/radiocheck-station" element={<CallScreen type="RADIO_CHECK_STATION" color="#009933" title="ROUTINE (SHIP TO STATION)" showPob={false} showSpeedHeading={false} showIncidentDetails={true} />} />
                </Routes>
            </Router>
        </ThemeContext.Provider>
    );
}