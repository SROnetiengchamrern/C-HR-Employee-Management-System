import { FormEvent, useEffect, useRef, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote, StatusBadge } from "../components/Ui";
import type { Benefit, Department, Employee, JobOpening, Paged, PerformanceReview, TemplateItem, TrainingSession, UserAccount } from "../types";

const noEmployees: Employee[] = [];

export function RecruitmentPage() {
  const { isHr } = useAuth();
  const [jobs, setJobs] = useState<JobOpening[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [form, setForm] = useState({ title: "", departmentId: "", openings: "1", description: "" });
  const [error, setError] = useState("");

  async function load() {
    const [openings, depts] = await Promise.all([api<JobOpening[]>("/api/recruitment"), api<Department[]>("/api/departments")]);
    setJobs(openings);
    setDepartments(depts);
    setForm((current) => ({ ...current, departmentId: current.departmentId || String(depts[0]?.id ?? "") }));
  }

  useEffect(() => { load().catch((caught: Error) => setError(caught.message)); }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      await api("/api/recruitment", { method: "POST", body: JSON.stringify({ ...form, departmentId: Number(form.departmentId), openings: Number(form.openings), status: "Active" }) });
      setForm({ ...form, title: "", description: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not add the opening.");
    }
  }

  const openCount = jobs.filter((job) => job.status === "Active").reduce((sum, job) => sum + job.openings, 0);

  return (
    <Module title="Recruitment" hint={`${openCount} active openings`}>
      <ErrorNote message={error} />
      {isHr && (
        <form className="entry-form row g-2" onSubmit={submit}>
          <div className="col-md-3"><input className="form-control" placeholder="Title" required value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></div>
          <div className="col-md-3">
            <select className="form-select" value={form.departmentId} onChange={(event) => setForm({ ...form, departmentId: event.target.value })}>
              {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
            </select>
          </div>
          <div className="col-md-2"><input className="form-control" type="number" min={1} value={form.openings} onChange={(event) => setForm({ ...form, openings: event.target.value })} /></div>
          <div className="col-md-3"><input className="form-control" placeholder="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></div>
          <div className="col-md-1"><button className="btn btn-primary w-100">Add</button></div>
        </form>
      )}
      <DataGrid data={jobs} empty="No openings yet.">
        <Column title="Role" data={(row: JobOpening) => `${row.title} ${row.description ?? ""}`}>
          {(_value: string, _type: string, row: JobOpening) => <>{row.title}<div className="muted">{row.description}</div></>}
        </Column>
        <Column title="Department" data="department" />
        <Column title="Openings" data="openings" />
        <Column title="Status" data="status">{(value: string) => <StatusBadge status={value} />}</Column>
      </DataGrid>
    </Module>
  );
}

export function OnboardingPage() {
  const { isHr } = useAuth();
  const [page, setPage] = useState<Paged<Employee> | null>(null);
  const [error, setError] = useState("");

  async function load() {
    setPage(await api<Paged<Employee>>("/api/employees?status=Probation&pageSize=50"));
  }

  useEffect(() => { load().catch((caught: Error) => setError(caught.message)); }, []);

  const activateRef = useRef<(id: number) => void>(() => undefined);
  async function activate(id: number) {
    await api(`/api/employees/${id}/status`, { method: "POST", body: JSON.stringify({ status: "Active" }) });
    await load();
  }
  activateRef.current = (id) => { activate(id).catch((caught: Error) => setError(caught.message)); };

  return (
    <Module title="Onboarding" hint="Employees still on probation">
      <ErrorNote message={error} />
      <DataGrid data={page?.items ?? noEmployees} empty="Nobody is waiting on probation.">
        <Column title="Employee" data={(row: Employee) => `${row.fullName} ${row.employeeCode}`}>
          {(_value: string, _type: string, row: Employee) => <>{row.fullName}<div className="muted">{row.employeeCode}</div></>}
        </Column>
        <Column title="Department" data="department" />
        <Column title="Hired" data="hireDate" />
        <Column title="" options={{ orderable: false, searchable: false }}>
          {(_value: unknown, type: string, row: Employee) => type !== "display" || !isHr ? "" : (
            <button className="btn btn-sm btn-primary" onClick={() => activateRef.current(row.id)}>Mark active</button>
          )}
        </Column>
      </DataGrid>
    </Module>
  );
}

export function TemplatesPage() {
  const [items, setItems] = useState<TemplateItem[]>([]);
  const [selected, setSelected] = useState<TemplateItem | null>(null);
  const [error, setError] = useState("");
  useEffect(() => { api<TemplateItem[]>("/api/templates").then(setItems).catch((caught: Error) => setError(caught.message)); }, []);
  return (
    <Module title="Templates" hint="Letters and messages HR can reuse">
      <ErrorNote message={error} />
      <div className="row g-3">
        <div className="col-md-5">
          {items.map((item) => (
            <button key={item.id} className="action w-100 mb-2 text-start" onClick={() => setSelected(item)}>
              <strong>{item.name}</strong><span className="muted">{item.category}</span>
            </button>
          ))}
        </div>
        <div className="col-md-7">
          <article className="panel">
            <h2>{selected?.name ?? "Select a template"}</h2>
            <p className="mt-3" style={{ whiteSpace: "pre-wrap" }}>{selected?.body ?? "Offer letters, leave notes, and certificates live here."}</p>
          </article>
        </div>
      </div>
    </Module>
  );
}

export function TrainingPage() {
  const { isHr } = useAuth();
  const [items, setItems] = useState<TrainingSession[]>([]);
  const [form, setForm] = useState({ title: "", startsAt: "", location: "", description: "" });
  const [error, setError] = useState("");
  async function load() { setItems(await api<TrainingSession[]>("/api/training")); }
  useEffect(() => { load().catch((caught: Error) => setError(caught.message)); }, []);
  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      await api("/api/training", { method: "POST", body: JSON.stringify(form) });
      setForm({ title: "", startsAt: "", location: "", description: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not add the session.");
    }
  }
  return (
    <Module title="Training" hint="Workshops and inductions">
      <ErrorNote message={error} />
      {isHr && (
        <form className="entry-form row g-2" onSubmit={submit}>
          <div className="col-md-3"><input className="form-control" required placeholder="Title" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></div>
          <div className="col-md-3"><input className="form-control" required type="datetime-local" value={form.startsAt} onChange={(event) => setForm({ ...form, startsAt: event.target.value })} /></div>
          <div className="col-md-2"><input className="form-control" placeholder="Location" value={form.location} onChange={(event) => setForm({ ...form, location: event.target.value })} /></div>
          <div className="col-md-3"><input className="form-control" placeholder="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></div>
          <div className="col-md-1"><button className="btn btn-primary w-100">Add</button></div>
        </form>
      )}
      <DataGrid data={items} empty="No training sessions.">
        <Column title="Session" data={(row: TrainingSession) => `${row.title} ${row.description ?? ""}`}>
          {(_value: string, _type: string, row: TrainingSession) => <>{row.title}<div className="muted">{row.description}</div></>}
        </Column>
        <Column title="When" data="startsAt">{(value: string) => new Date(value).toLocaleString()}</Column>
        <Column title="Location" data="location" />
      </DataGrid>
    </Module>
  );
}

export function BenefitsPage() {
  const [items, setItems] = useState<Benefit[]>([]);
  const [error, setError] = useState("");
  useEffect(() => { api<Benefit[]>("/api/benefits").then(setItems).catch((caught: Error) => setError(caught.message)); }, []);
  return (
    <Module title="Benefits" hint="Plans available to employees">
      <ErrorNote message={error} />
      <div className="row g-3">
        {items.map((item) => (
          <div className="col-md-6" key={item.id}>
            <article className="panel h-100">
              <h2>{item.name}</h2>
              <p className="mt-2">{item.description}</p>
              <p className="muted mt-2">Eligibility: {item.eligibility}</p>
            </article>
          </div>
        ))}
      </div>
    </Module>
  );
}

export function PerformancePage() {
  const [items, setItems] = useState<PerformanceReview[]>([]);
  const [error, setError] = useState("");
  useEffect(() => { api<PerformanceReview[]>("/api/performance").then(setItems).catch((caught: Error) => setError(caught.message)); }, []);
  return (
    <Module title="Performance" hint="Review cycle">
      <ErrorNote message={error} />
      <DataGrid data={items} empty="No reviews yet.">
        <Column title="Employee" data={(row: PerformanceReview) => `${row.employeeName} ${row.department}`}>
          {(_value: string, _type: string, row: PerformanceReview) => <>{row.employeeName}<div className="muted">{row.department}</div></>}
        </Column>
        <Column title="Period" data="year">{(_value: number, _type: string, row: PerformanceReview) => `${row.year} ${row.period}`}</Column>
        <Column title="Rating" data="rating">{(value: number) => <span className="stars">{"★".repeat(value)}{"☆".repeat(Math.max(0, 5 - value))}</span>}</Column>
        <Column title="Summary" data="summary" />
        <Column title="Status" data="status">{(value: string) => <StatusBadge status={value} />}</Column>
      </DataGrid>
    </Module>
  );
}

export function AdminPage() {
  const { isAdmin } = useAuth();
  const [users, setUsers] = useState<UserAccount[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [form, setForm] = useState({ fullName: "", email: "", password: "", role: "Employee", employeeId: "" });
  const [error, setError] = useState("");

  async function load() {
    const [accounts, directory] = await Promise.all([
      api<UserAccount[]>("/api/admin/users"),
      api<Paged<Employee>>("/api/employees?pageSize=1000")
    ]);
    setUsers(accounts);
    setEmployees(directory.items);
  }

  useEffect(() => {
    if (isAdmin) load().catch((caught: Error) => setError(caught.message));
  }, [isAdmin]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      await api("/api/admin/users", {
        method: "POST",
        body: JSON.stringify({ ...form, employeeId: form.employeeId ? Number(form.employeeId) : null })
      });
      setForm({ fullName: "", email: "", password: "", role: "Employee", employeeId: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not create the user.");
    }
  }

  if (!isAdmin) return <Module title="Administration"><p>Only an administrator can manage logins.</p></Module>;

  return (
    <Module title="Administration" hint="Identity accounts and roles">
      <ErrorNote message={error} />
      <form className="entry-form row g-2" onSubmit={submit}>
        <div className="col-md-3"><input className="form-control" required placeholder="Full name" value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} /></div>
        <div className="col-md-3"><input className="form-control" required type="email" placeholder="Email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></div>
        <div className="col-md-2"><input className="form-control" required type="password" placeholder="Password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /></div>
        <div className="col-md-2">
          <select className="form-select" value={form.role} onChange={(event) => setForm({ ...form, role: event.target.value })}>
            {["Admin", "HR", "Manager", "Employee"].map((role) => <option key={role}>{role}</option>)}
          </select>
        </div>
        <div className="col-md-2">
          <select className="form-select" value={form.employeeId} onChange={(event) => setForm({ ...form, employeeId: event.target.value })}>
            <option value="">No employee link</option>
            {employees.map((employee) => <option key={employee.id} value={employee.id}>{employee.employeeCode}</option>)}
          </select>
        </div>
        <div className="col-12"><button className="btn btn-primary">Create login</button></div>
      </form>
      <DataGrid data={users} empty="No logins yet.">
        <Column title="Name" data="fullName" />
        <Column title="Email" data="email" />
        <Column title="Roles" data="roles">{(value: string[]) => value.join(", ")}</Column>
        <Column title="Employee" data="employeeCode">{(value: string | null) => value ?? "—"}</Column>
      </DataGrid>
    </Module>
  );
}

function Module({ title, hint, children }: { title: string; hint?: string; children: ReactNode }) {
  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> {title}</div>
      <div className="page-head"><h2>{title}</h2>{hint && <span className="muted">{hint}</span>}</div>
      {children}
    </>
  );
}
