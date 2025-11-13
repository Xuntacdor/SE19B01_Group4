import React, { useEffect, useState } from "react";
import { getSignInHistory } from "../../../Services/UserApi";
import NothingFound from "../../../Components/Nothing/NothingFound";
import { Clock, Globe, Monitor } from "lucide-react";
import useAuth from "../../../Hook/UseAuth";
import { format, formatDistanceToNow } from "date-fns";
import "./SignInTab.css";

export default function SignInTab() {
  const { user } = useAuth();
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // -----------------------------
  // FIX TIMESTAMP (BACKEND sends VN-time but JS reads as UTC)
  // -----------------------------
  function parseServerTime(raw) {
    if (!raw) return new Date();

    const d = new Date(raw);

    // JS hiểu "2025-11-13T01:45:59" là UTC → bị +7h sai
    // Ta bù trừ timezone offset để đưa về đúng local time backend
    return new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  }

  // -----------------------------
  // FORMAT IP ADDRESS
  // -----------------------------
  function formatIpAddress(ip) {
    if (!ip) return "Unknown";

    ip = ip.trim();

    if (ip === "::1") return "localhost";

    const mapped = ip.match(/^::ffff:(\d+\.\d+\.\d+\.\d+)$/);
    if (mapped) return mapped[1];

    const ipv4 = ip.match(/^(\d{1,3}\.){3}\d{1,3}$/);
    if (ipv4) return ip;

    if (ip.includes(":")) {
      return ip.replace(/^::ffff:/, "").toLowerCase();
    }

    return ip;
  }

  // -----------------------------
  // FORMAT USER-AGENT
  // -----------------------------
  function simplifyDeviceInfo(ua) {
    if (!ua) return "Unknown Device";

    const lower = ua.toLowerCase();
    let os = "Unknown OS";
    let browser = "Unknown Browser";

    if (lower.includes("windows")) os = "Windows";
    else if (lower.includes("mac")) os = "macOS";
    else if (lower.includes("android")) os = "Android";
    else if (lower.includes("iphone") || lower.includes("ios")) os = "iOS";
    else if (lower.includes("linux")) os = "Linux";

    if (lower.includes("chrome")) browser = "Chrome";
    else if (lower.includes("safari") && !lower.includes("chrome"))
      browser = "Safari";
    else if (lower.includes("firefox")) browser = "Firefox";
    else if (lower.includes("edg")) browser = "Edge";

    return `${os} / ${browser}`;
  }

  // -----------------------------
  // FETCH DATA
  // -----------------------------
  useEffect(() => {
    if (!user?.userId) return;

    setLoading(true);

    getSignInHistory(user.userId)
      .then((res) => {
        const data =
          res.data?.map((x) => ({
            signInTime: parseServerTime(x.signedInAt), // <= FIXED
            ipAddress: formatIpAddress(x.ipAddress),
            device: simplifyDeviceInfo(x.deviceInfo),
          })) || [];

        setHistory(data);
        setError(null);
      })
      .catch((err) => {
        console.error("Failed to load sign-in history:", err);
        setError("Failed to fetch sign-in history.");
      })
      .finally(() => setLoading(false));
  }, [user?.userId]);

  // -----------------------------
  // UI
  // -----------------------------

  if (loading) return <p>Loading sign-in history...</p>;

  if (error)
    return (
      <NothingFound
        title="Error loading history"
        message={error}
        imageSrc="/src/assets/error.png"
      />
    );

  if (!history.length)
    return (
      <NothingFound
        title="No Sign-In Records"
        message="You haven’t signed in recently or no records are available."
        imageSrc="/src/assets/empty_history.png"
      />
    );

  return (
    <div className="signin-table-container">
      <h2>Sign-In History</h2>

      <div className="signin-table-wrapper">
        <table className="signin-table">
          <thead>
            <tr>
              <th>
                <Clock size={16} /> Date & Time
              </th>
              <th>
                <Globe size={16} /> IP Address
              </th>
              <th>
                <Monitor size={16} /> Device
              </th>
            </tr>
          </thead>

          <tbody>
            {history.map((record, i) => (
              <tr key={i}>
                <td>
                  <div className="time-cell">
                    <span className="main-time">
                      {format(record.signInTime, "PPpp")}
                    </span>

                    <span className="sub-time">
                      (
                      {formatDistanceToNow(record.signInTime, {
                        addSuffix: true,
                      })}
                      )
                    </span>
                  </div>
                </td>

                <td>{record.ipAddress}</td>

                <td className="device-cell">{record.device}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
