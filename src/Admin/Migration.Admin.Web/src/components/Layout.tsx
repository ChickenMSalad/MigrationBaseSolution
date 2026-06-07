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
  Workflow
} from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";

const nav = [
  { to: "/", label: "Dashboard", icon: Home, end: true },
  { to: "/projects", label: "Projects", icon: Workflow },
  { to: "/runtime-dashboard", label: "Runtime Dashboard", icon: Gauge },
  { to: "/execution-sessions", label: "Execution Sessions", icon: Workflow },
  { to: "/failure-retry", label: "Failure Retry", icon: RefreshCcw },
  { to: "/manifest-builder", label: "Manifest Builder", icon: FileSpreadsheet },
  { to: "/mapping-builder", label: "Mapping Builder", icon: Map },
  { to: "/taxonomy-builder", label: "Taxonomy Builder", icon: Tags },
  { to: "/artifacts", label: "Artifacts", icon: Archive },
  { to: "/operations/operational-events", label: "Operational Events", icon: Activity },
  { to: "/connector-configuration", label: "Connector Configuration", icon: Settings },
  { to: "/credentials", label: "Credentials", icon: KeyRound }
];

export function Layout() {
  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brandIcon">M</div>
          <div>
            <strong>Migration Admin</strong>
            <span>Control Plane v1</span>
          </div>
        </div>

        <nav>
          {nav.map((item) => {
            const Icon = item.icon;

            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) => isActive ? "navItem active" : "navItem"}
              >
                <Icon size={18} />
                {item.label}
              </NavLink>
            );
          })}
        </nav>

        <div className="sidebarFooter">API-driven shell</div>
      </aside>

      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
