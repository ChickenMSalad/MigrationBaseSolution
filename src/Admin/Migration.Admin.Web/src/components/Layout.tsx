import {
  Activity,
  Archive,
  FileSpreadsheet,
  Gauge,
  Home,
  KeyRound,
  Map,
  RefreshCcw,
  Settings,
  Tags,
  Workflow,
} from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";

const nav = [
  { to: "/", label: "Dashboard", icon: Home, end: true },
  { to: "/projects", label: "Projects", icon: Workflow },
  { to: "/command-center", label: "Command Center", icon: Gauge },
  { to: "/runtime-dashboard", label: "Runtime Dashboard", icon: Gauge },
  { to: "/runs", label: "Runs", icon: Activity },
  { to: "/execution-sessions", label: "Execution Sessions", icon: Workflow },
  { to: "/execution-worker-telemetry", label: "Worker Telemetry", icon: Workflow },
  { to: "/failure-retry", label: "Failure Retry", icon: RefreshCcw },
  { to: "/operations/operational-events", label: "Operational Events", icon: Activity },
  { to: "/audit-trail", label: "Audit Trail", icon: Activity },
  { to: "/notification-routing", label: "Notification Routing", icon: Activity },
  { to: "/manifest-builder", label: "Manifest Builder", icon: FileSpreadsheet },
  { to: "/mapping-builder", label: "Mapping Builder", icon: Map },
  { to: "/taxonomy-builder", label: "Taxonomy Builder", icon: Tags },
  { to: "/artifacts", label: "Artifacts", icon: Archive },
  { to: "/connectors", label: "Connectors", icon: Settings },
  { to: "/connector-configuration", label: "Connector Configuration", icon: Settings },
  { to: "/credentials", label: "Credentials", icon: KeyRound },
  { to: "/credential-vault", label: "Credential Vault", icon: KeyRound },
];

export function Layout() {
  return (
    <div className="appShell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brandMark">M</div>
          <div>
            <div className="brandTitle">Migration Admin</div>
            <div className="brandSubtitle">Control Plane v1</div>
          </div>
        </div>

        <nav className="navList" aria-label="Primary navigation">
          {nav.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) => (isActive ? "navItem active" : "navItem")}
              >
                <Icon size={18} aria-hidden="true" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>

        <div className="sidebarFooter">API-driven shell</div>
      </aside>

      <main className="mainContent">
        <Outlet />
      </main>
    </div>
  );
}
