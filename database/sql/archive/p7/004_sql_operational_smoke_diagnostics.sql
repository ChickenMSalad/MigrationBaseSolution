/*
P7.7E SQL Operational Smoke Diagnostics
Read-only diagnostic script. Does not insert, update, delete, or create objects.
*/

set nocount on;

select
    schema_name(t.schema_id) as SchemaName,
    t.name as TableName,
    p.rows as ApproximateRows
from sys.tables t
inner join sys.partitions p on p.object_id = t.object_id and p.index_id in (0, 1)
where schema_name(t.schema_id) in ('migration', 'dbo')
  and (
       t.name like '%Run%'
    or t.name like '%Manifest%'
    or t.name like '%WorkItem%'
    or t.name like '%Failure%'
    or t.name like '%Checkpoint%'
    or t.name like '%Mapping%'
  )
order by schema_name(t.schema_id), t.name;

select
    schema_name(t.schema_id) as SchemaName,
    t.name as TableName,
    c.name as ColumnName,
    ty.name as DataType,
    c.is_nullable as IsNullable
from sys.tables t
inner join sys.columns c on c.object_id = t.object_id
inner join sys.types ty on ty.user_type_id = c.user_type_id
where schema_name(t.schema_id) in ('migration', 'dbo')
  and (
       t.name like '%Run%'
    or t.name like '%Manifest%'
    or t.name like '%WorkItem%'
    or t.name like '%Failure%'
    or t.name like '%Checkpoint%'
    or t.name like '%Mapping%'
  )
order by schema_name(t.schema_id), t.name, c.column_id;
