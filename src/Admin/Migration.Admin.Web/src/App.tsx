import { Navigate, Route, Routes } from "react-router-dom";

import { Layout } from "./components/Layout";
import { Artifacts } from "./pages/Artifacts";
import { Connectors } from "./pages/Connectors";
import { Credentials } from "./pages/Credentials";
import { Dashboard } from "./pages/Dashboard";
import { MappingBuilder } from "./pages/MappingBuilder";
import { Preflight } from "./pages/Preflight";
import { ProjectDetail } from "./pages/ProjectDetail";
import { Projects } from "./pages/Projects";
import { RunDetail } from "./pages/RunDetail";
import { Runs } from "./pages/Runs";
import { ManifestBuilder } from "./pages/ManifestBuilder";
import { TaxonomyBuilder } from "./pages/TaxonomyBuilder";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="projects" element={<Projects />} />
        <Route path="projects/:projectId" element={<ProjectDetail />} />
        <Route path="projects/:projectId/preflight" element={<Preflight />} />
        <Route path="runs" element={<Runs />} />
        <Route path="runs/:runId" element={<RunDetail />} />
        <Route path="connectors" element={<Connectors />} />
        <Route path="credentials" element={<Credentials />} />
        <Route path="artifacts" element={<Artifacts />} />
        <Route path="mapping-builder" element={<MappingBuilder />} />
        <Route path="manifest-builder" element={<ManifestBuilder />} />
        <Route path="taxonomy-builder" element={<TaxonomyBuilder />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}