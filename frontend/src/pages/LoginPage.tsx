import { FormEvent, useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../auth";
import { ErrorNote } from "../components/Ui";

const demos = [
  ["Admin", "amila@corehr.lk", "Admin@123456"],
  ["HR", "hr@corehr.lk", "Hr@123456"],
  ["Manager", "manager@corehr.lk", "Manager@123456"],
  ["Employee", "employee@corehr.lk", "Employee@123456"]
];

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("amila@corehr.lk");
  const [password, setPassword] = useState("Admin@123456");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/" replace />;

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await login(email, password);
      navigate("/");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Sign in failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="login-shell">
      <section className="login-hero">
        <div>
          <div className="brand text-white"><span className="text-white">Core</span><span>HR</span></div>
          <h1 className="mt-4">People, attendance, and leave in one desk.</h1>
          <p className="mt-3 col-lg-8">Run the admin dashboard, keep the employee directory current, and close approvals without leaving the browser.</p>
        </div>
        <p>HrEmployeeManagementSystem</p>
      </section>
      <section className="login-panel">
        <form className="login-card" onSubmit={submit}>
          <h2 className="mb-1">Sign in</h2>
          <p className="muted mb-3">Use your CoreHR account.</p>
          <ErrorNote message={error} />
          <label className="form-label" htmlFor="email">Email</label>
          <input id="email" className="form-control mb-3" type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
          <label className="form-label" htmlFor="password">Password</label>
          <input id="password" className="form-control mb-3" type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
          <button className="btn btn-primary w-100" disabled={busy}>{busy ? "Signing in…" : "Sign in"}</button>
          <div className="demo-box mt-4">
            <strong>Demo accounts</strong>
            {demos.map(([role, demoEmail, demoPassword]) => (
              <div key={demoEmail} className="d-flex justify-content-between gap-2 mt-2">
                <span>{role}</span>
                <button type="button" onClick={() => { setEmail(demoEmail); setPassword(demoPassword); }}>{demoEmail}</button>
              </div>
            ))}
          </div>
        </form>
      </section>
    </div>
  );
}
