import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import { Column, DataGrid } from "../components/DataGrid";
import { ErrorNote } from "../components/Ui";
import type { Department, Position } from "../types";

export function OrganizationPage() {
  const { isHr } = useAuth();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [error, setError] = useState("");
  const [departmentForm, setDepartmentForm] = useState({ name: "", code: "" });
  const [positionForm, setPositionForm] = useState({ title: "", departmentId: "" });

  async function load() {
    const [nextDepartments, nextPositions] = await Promise.all([
      api<Department[]>("/api/departments"),
      api<Position[]>("/api/positions")
    ]);
    setDepartments(nextDepartments);
    setPositions(nextPositions);
    setPositionForm((current) => ({ ...current, departmentId: current.departmentId || String(nextDepartments[0]?.id ?? "") }));
  }

  useEffect(() => {
    load().catch((caught: Error) => setError(caught.message));
  }, []);

  async function addDepartment(event: FormEvent) {
    event.preventDefault();
    try {
      await api("/api/departments", { method: "POST", body: JSON.stringify({ ...departmentForm, isActive: true }) });
      setDepartmentForm({ name: "", code: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not add the department.");
    }
  }

  async function addPosition(event: FormEvent) {
    event.preventDefault();
    try {
      await api("/api/positions", { method: "POST", body: JSON.stringify({ ...positionForm, departmentId: Number(positionForm.departmentId) }) });
      setPositionForm({ ...positionForm, title: "" });
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not add the position.");
    }
  }

  return (
    <>
      <div className="crumb"><Link to="/">Home</Link> <span>›</span> Organization</div>
      <ErrorNote message={error} />
      <div className="row g-3">
        <div className="col-lg-6">
          <article className="panel">
            <h2 className="mb-3">Departments</h2>
            {isHr && (
              <form className="row g-2 mb-3" onSubmit={addDepartment}>
                <div className="col-7"><input className="form-control" placeholder="Name" required value={departmentForm.name} onChange={(event) => setDepartmentForm({ ...departmentForm, name: event.target.value })} /></div>
                <div className="col-3"><input className="form-control" placeholder="Code" required value={departmentForm.code} onChange={(event) => setDepartmentForm({ ...departmentForm, code: event.target.value })} /></div>
                <div className="col-2"><button className="btn btn-primary w-100">Add</button></div>
              </form>
            )}
            <DataGrid data={departments} empty="No departments yet.">
              <Column title="Name" data="name">
                {(value: string, _type: string, row: Department) => <Link to={`/people?departmentId=${row.id}`}>{value}</Link>}
              </Column>
              <Column title="Code" data="code" />
              <Column title="People" data="employeeCount" />
            </DataGrid>
          </article>
        </div>
        <div className="col-lg-6" id="positions">
          <article className="panel">
            <h2 className="mb-3">Positions</h2>
            {isHr && (
              <form className="row g-2 mb-3" onSubmit={addPosition}>
                <div className="col-5"><input className="form-control" placeholder="Title" required value={positionForm.title} onChange={(event) => setPositionForm({ ...positionForm, title: event.target.value })} /></div>
                <div className="col-5">
                  <select className="form-select" value={positionForm.departmentId} onChange={(event) => setPositionForm({ ...positionForm, departmentId: event.target.value })}>
                    {departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
                  </select>
                </div>
                <div className="col-2"><button className="btn btn-primary w-100">Add</button></div>
              </form>
            )}
            <DataGrid data={positions} empty="No positions yet.">
              <Column title="Title" data="title" />
              <Column title="Department" data="department" />
            </DataGrid>
          </article>
        </div>
      </div>
    </>
  );
}
