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

  useEffect(() => {
    if (!user?.userId) return;
    setLoading(true);
    getSignInHistory(user.userId)
      .then((res) => {
        const data =
          res.data?.map((x) => ({
            signInTime: x.signedInAt,
            ipAddress: x.ipAddress,
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

  // === Helper function to simplify user agent ===
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
                      {format(new Date(record.signInTime), "PPpp")}
                    </span>
                    <span className="sub-time">
                      (
                      {formatDistanceToNow(new Date(record.signInTime), {
                        addSuffix: true,
                      })}
                      )
                    </span>
                  </div>
                </td>
                <td>{record.ipAddress || "Unknown"}</td>
                <td className="device-cell">{record.device}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
