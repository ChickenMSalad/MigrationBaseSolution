export type OperationalSqlHealth = {
  status: string;
  databaseName: string | null;
  verifiedTables: string[];
  missingTables: string[];
  message: string;
};
