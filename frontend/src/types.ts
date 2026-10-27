export type EmploymentStatus = "Active" | "Probation" | "OnLeave" | "Resigned" | "Terminated";
export type LeaveStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  employeeId: number | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string | null;
  dateOfBirth: string;
  hireDate: string;
  status: EmploymentStatus;
  departmentId: number;
  department: string;
  departmentCode: string;
  positionId: number;
  position: string;
  managerId: number | null;
  managerName: string | null;
}

export interface LeaveBalance {
  leaveTypeId: number;
  leaveType: string;
  allowance: number;
  usedDays: number;
}

export interface EmployeeDetail {
  employee: Employee;
  leaveBalances: LeaveBalance[];
}

export interface Department {
  id: number;
  name: string;
  code: string;
  isActive: boolean;
  employeeCount: number;
}

export interface Position {
  id: number;
  title: string;
  departmentId: number;
  department: string;
}

export interface AttendanceRow {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  department: string;
  workDate: string;
  checkIn: string | null;
  checkOut: string | null;
  hours: number | null;
}

export interface LeaveType {
  id: number;
  name: string;
  defaultDays: number;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  department: string;
  leaveTypeId: number;
  leaveType: string;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: LeaveStatus;
  reviewComment: string | null;
  createdAt: string;
}

export interface DepartmentCount {
  id: number;
  name: string;
  code: string;
  count: number;
}

export interface TrainingItem {
  id: number;
  title: string;
  startsAt: string;
  location: string | null;
  isToday: boolean;
}

export interface DashboardData {
  totalEmployees: number;
  headcountChangePercent: number;
  presentToday: number;
  attendancePercent: number;
  pendingApprovals: number;
  openPositions: number;
  departments: DepartmentCount[];
  birthdayCount: number;
  birthdayNames: string[];
  anniversaryCount: number;
  anniversaryNames: string[];
  training: TrainingItem[];
  birthdayDistribution: number[];
  pendingLeaves: LeaveRequest[];
  newHires: { month: number; count: number }[];
  hiresYear: number;
  hiresYears: number[];
}

export interface HeadcountReport {
  total: number;
  active: number;
  probation: number;
  onLeave: number;
  resigned: number;
  terminated: number;
  byDepartment: DepartmentCount[];
}

export interface AttendanceReport {
  from: string;
  to: string;
  records: number;
  averageHours: number;
  rows: AttendanceRow[];
}

export interface LeaveReport {
  from: string;
  to: string;
  pending: number;
  approved: number;
  rejected: number;
  cancelled: number;
  totalDays: number;
  rows: LeaveRequest[];
}

export interface NotificationItem {
  id: number;
  title: string;
  message: string;
  createdAt: string;
  isRead: boolean;
}

export interface JobOpening {
  id: number;
  title: string;
  departmentId: number;
  department: string;
  openings: number;
  status: string;
  description: string | null;
}

export interface TrainingSession {
  id: number;
  title: string;
  startsAt: string;
  location: string | null;
  description: string | null;
}

export interface TemplateItem {
  id: number;
  name: string;
  category: string;
  body: string;
}

export interface Benefit {
  id: number;
  name: string;
  description: string;
  eligibility: string;
}

export interface PerformanceReview {
  id: number;
  employeeId: number;
  employeeName: string;
  department: string;
  year: number;
  period: string;
  rating: number;
  summary: string;
  status: string;
}

export interface UserAccount {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  employeeId: number | null;
  employeeCode: string | null;
}
