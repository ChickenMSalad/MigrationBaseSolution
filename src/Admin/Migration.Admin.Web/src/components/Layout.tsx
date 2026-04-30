import { Activity, Amphora, Boxes, FolderKanban, GitBranch, Home, KeyRound, Map, PlugZap, FileSpreadsheet } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";

const nav = [
  { to: "/", label: "Dashboard", icon: Home, end: true },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  { to: "/runs", label: "Runs", icon: Activity },
  { to: "/connectors", label: "Connectors", icon: PlugZap },
  { to: "/credentials", label: "Credentials", icon: KeyRound },
  { to: "/artifacts", label: "Artifacts", icon: Amphora },
  { to: "/mapping-builder", label: "Mapping Builder", icon: Map },
  { to: "/manifest-builder", label: "Manifest Builder", icon: FileSpreadsheet },
];

export function Layout() {
  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brandIcon"><Boxes size={22} /></div>
          <div>
            <div className="brandTitle">Migration Admin</div>
            <div className="brandSub">Control Plane v1</div>
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

        <div className="sidebarFooter">
          <GitBranch size={15} /> API-driven shell
        </div>
      </aside>

      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
