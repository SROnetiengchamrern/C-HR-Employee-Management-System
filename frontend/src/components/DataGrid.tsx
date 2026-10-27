import { useMemo, type ReactNode } from "react";
import type { Config } from "datatables.net";
import DataTable, { Column } from "datatables.net-react";
import DataTablesLib from "datatables.net-bs5";

DataTable.use(DataTablesLib);

const gridOptions: Config = {
  pageLength: 10,
  lengthMenu: [10, 25, 50, 100],
  autoWidth: false,
  order: [[0, "asc"]],
  language: {
    search: "",
    searchPlaceholder: "Search",
    lengthMenu: "Show _MENU_",
    info: "_START_–_END_ of _TOTAL_",
    infoEmpty: "No records",
    emptyTable: "No records to show.",
    zeroRecords: "No matching records.",
    paginate: { previous: "Previous", next: "Next" }
  }
};

export function DataGrid({ data, children, empty }: { data: object[]; children: ReactNode; empty?: string }) {
  const options = useMemo<Config>(() => ({
    ...gridOptions,
    language: {
      ...gridOptions.language,
      emptyTable: empty ?? gridOptions.language?.emptyTable,
      zeroRecords: empty ? "No matching records." : gridOptions.language?.zeroRecords
    }
  }), [empty]);

  return (
    <div className="table-card data-grid">
      <DataTable data={data} className="table table-hover align-middle w-100" options={options}>
        {children}
      </DataTable>
    </div>
  );
}

export { Column };
