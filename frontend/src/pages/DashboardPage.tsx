import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Bar, Line } from "react-chartjs-2";
import { api } from "../api";
import { ErrorNote } from "../components/Ui";
import { formatChange, monthLabels } from "../lib/format";
import type { DashboardData } from "../types";

export function DashboardPage() {
  const navigate = useNavigate();
  const [year, setYear] = useState<number | "">("");
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState("");
  const [listView, setListView] = useState(false);

  useEffect(() => {
    let active = true;
    const query = year === "" ? "" : `?hiresYear=${year}`;
    api<DashboardData>(`/api/dashboard${query}`)
      .then((result) => {
        if (!active) return;
        setData(result);
        setError("");
      })
      .catch((caught: Error) => {
        if (active) setError(caught.message);
      });
    return () => {
      active = false;
    };
  }, [year]);

  if (error && !data) return <ErrorNote message={error} />;
  if (!data) return <p>Loading dashboard…</p>;

  const maxDepartment = Math.max(...data.departments.map((item) => item.count), 1);

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Dashboard</div>
      <ErrorNote message={error} />
      <section className="stat-grid">
        <article className="stat stat-blue">
          <div>
            <div className="stat-label">Total Employees</div>
            <div className="stat-value">{data.totalEmployees}</div>
            <div className="stat-hint">{formatChange(data.headcountChangePercent)}</div>
          </div>
          <div className="stat-icon"><i className="bi bi-people" /></div>
        </article>
        <article className="stat stat-green">
          <div>
            <div className="stat-label">Today's Attendance</div>
            <div className="stat-value">{data.attendancePercent}%</div>
            <div className="stat-hint">Present: {data.presentToday} of {data.totalEmployees}</div>
          </div>
          <div className="stat-icon"><i className="bi bi-calendar-check" /></div>
        </article>
        <article className="stat stat-orange">
          <div>
            <div className="stat-label">Pending Approvals</div>
            <div className="stat-value">{data.pendingApprovals}</div>
            <div className="stat-hint">Leaves + Requests</div>
          </div>
          <div className="stat-icon"><i className="bi bi-clock" /></div>
        </article>
        <article className="stat stat-purple">
          <div>
            <div className="stat-label">Open Positions</div>
            <div className="stat-value">{data.openPositions}</div>
            <div className="stat-hint">Active recruiting</div>
          </div>
          <div className="stat-icon"><i className="bi bi-briefcase" /></div>
        </article>
      </section>

      <section className="dash-top">
        <article className="panel">
          <div className="panel-head">
            <h2>Department Overview</h2>
            <button className="ghost-btn" onClick={() => setListView((value) => !value)}><i className="bi bi-arrow-left-right" /> Switch View</button>
          </div>
          {listView ? (
            <div className="rank">
              {data.departments.map((department) => (
                <button key={department.id} className="rank-row btn p-0 text-start" onClick={() => navigate(`/people?departmentId=${department.id}`)}>
                  <span>{department.code}</span>
                  <span className="rank-bar"><span style={{ width: `${(department.count / maxDepartment) * 100}%` }} /></span>
                  <strong>{department.count}</strong>
                </button>
              ))}
            </div>
          ) : (
            <div className="chart-box">
              <Bar
                data={{
                  labels: data.departments.map((department) => department.code),
                  datasets: [{ data: data.departments.map((department) => department.count), backgroundColor: "#5b8def", borderRadius: 6, maxBarThickness: 28 }]
                }}
                options={{
                  responsive: true,
                  maintainAspectRatio: false,
                  plugins: { legend: { display: false } },
                  onClick: (_event, elements) => {
                    const index = elements[0]?.index;
                    if (index === undefined) return;
                    const department = data.departments[index];
                    if (department) navigate(`/people?departmentId=${department.id}`);
                  },
                  scales: {
                    x: { grid: { display: false }, ticks: { maxRotation: 50, minRotation: 40, font: { size: 10 } } },
                    y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: "#eef2f7" } }
                  }
                }}
              />
            </div>
          )}
          <p className="chart-note">Click departments for details</p>
        </article>

        <article className="panel">
          <div className="panel-head">
            <h2>Upcoming Events</h2>
            <Link to="/training" className="ghost-btn" aria-label="Open training"><i className="bi bi-calendar3" /></Link>
          </div>
          <div className="event event-pink">
            <div><b><i className="bi bi-balloon me-1" /> Birthdays This Week</b><span>{data.birthdayCount} employees celebrating</span></div>
            <span className="count-pill">{data.birthdayCount}</span>
          </div>
          <div className="event event-blue">
            <div>
              <b><i className="bi bi-award me-1" /> Work Anniversaries</b>
              <span>{data.anniversaryNames[0] ?? "5+ years"}</span>
            </div>
            <span className="count-pill">{data.anniversaryCount}</span>
          </div>
          {data.training.map((session) => (
            <div className="event event-green" key={session.id}>
              <div>
                <b><i className="bi bi-mortarboard me-1" /> Training Sessions</b>
                <span>{session.title}{session.isToday ? " · Today" : ""}</span>
              </div>
              <span className="count-pill">{session.isToday ? "Today" : new Date(session.startsAt).toLocaleDateString(undefined, { month: "short", day: "numeric" })}</span>
            </div>
          ))}
          <h3 className="mini-title">Monthly Birthday Distribution</h3>
          <div className="chart-box" style={{ height: 140 }}>
            <Bar
              data={{
                labels: monthLabels,
                datasets: [{ data: data.birthdayDistribution, backgroundColor: "#f5b14a", borderRadius: 4, maxBarThickness: 18 }]
              }}
              options={{
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                  x: { grid: { display: false }, ticks: { font: { size: 10 } } },
                  y: { display: false, beginAtZero: true }
                }
              }}
            />
          </div>
        </article>
      </section>

      <section className="dash-bottom">
        <article className="panel">
          <div className="panel-head">
            <h2>Pending Approvals</h2>
            <span className="count-pill">{data.pendingApprovals}</span>
          </div>
          {data.pendingLeaves.length === 0 && <div className="empty">No pending approvals</div>}
          {data.pendingLeaves.map((leave) => (
            <div className="leave-mini" key={leave.id}>
              <div><strong>{leave.employeeName}</strong><div className="muted">{leave.leaveType} · {leave.startDate}</div></div>
              <span>{leave.days}d</span>
            </div>
          ))}
          <Link to="/leave">View All Requests →</Link>
        </article>
        <article className="panel">
          <div className="panel-head">
            <h2>New Hires</h2>
            <select className="form-select form-select-sm w-auto" value={data.hiresYear} onChange={(event) => setYear(Number(event.target.value))}>
              {data.hiresYears.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </div>
          <div className="chart-box">
            <Line
              data={{
                labels: monthLabels,
                datasets: [{
                  data: data.newHires.map((item) => item.count),
                  borderColor: "#7c6cf0",
                  backgroundColor: "rgba(124,108,240,.15)",
                  fill: true,
                  tension: 0.35,
                  pointRadius: 3
                }]
              }}
              options={{
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                  x: { grid: { display: false } },
                  y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: "#eef2f7" } }
                }
              }}
            />
          </div>
        </article>
        <article className="panel">
          <div className="panel-head"><h2>Quick Actions</h2></div>
          <div className="actions">
            <button className="action blue" onClick={() => navigate("/people?new=1")}><i className="bi bi-person-plus" />Add Employee</button>
            <button className="action green" onClick={() => navigate("/attendance")}><i className="bi bi-check2-circle" />Attendance</button>
          </div>
        </article>
      </section>
    </>
  );
}
