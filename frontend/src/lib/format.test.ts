import { describe, expect, it } from "vitest";
import { formatChange, leaveDays, toQuery } from "./format";

describe("query strings", () => {
  it("skips empty filters", () => {
    expect(toQuery({ search: "amila", departmentId: 4, status: "", page: 1 })).toBe("search=amila&departmentId=4&page=1");
  });
});

describe("leave days", () => {
  it("counts the start and end dates", () => {
    expect(leaveDays("2027-04-12", "2027-04-13")).toBe(2);
    expect(leaveDays("2027-04-13", "2027-04-12")).toBe(0);
  });
});

describe("headcount change", () => {
  it("prefixes an increase", () => {
    expect(formatChange(2.5)).toBe("↑ +2.5% from last month");
  });
});
