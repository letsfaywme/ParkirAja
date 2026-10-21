-- Run only through Initialize-Database.ps1 (psql variable runtime_role is required).
-- No startup migrations. Any incompatibility aborts and rolls back this entire file.
-- Username writers MUST lock pg_advisory_xact_lock(736192004::bigint), check
-- BOTH admins and jukir (including inactive users), then insert/update in that
-- transaction. Authentication MUST reject ambiguous cross-table matches.
BEGIN;
SELECT pg_advisory_xact_lock(736192004::bigint);
CREATE TEMP TABLE bootstrap_options (runtime_role text NOT NULL) ON COMMIT DROP;
INSERT INTO bootstrap_options VALUES (:'runtime_role');

DO $bootstrap$
DECLARE
    runtime_record pg_catalog.pg_roles%ROWTYPE;
    versions integer[];
BEGIN
    SELECT * INTO runtime_record FROM pg_catalog.pg_roles
    WHERE rolname = (SELECT runtime_role FROM bootstrap_options);
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Runtime role must already exist.';
    END IF;
    IF runtime_record.rolname = current_user OR runtime_record.rolsuper
       OR runtime_record.rolcreatedb OR runtime_record.rolcreaterole
       OR runtime_record.rolreplication OR runtime_record.rolbypassrls THEN
        RAISE EXCEPTION 'Runtime role must be separate and unprivileged.';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'parking') THEN
        IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'parking' AND c.relname = 'schema_version' AND c.relkind = 'r'
        ) THEN
            RAISE EXCEPTION 'Existing parking schema has no compatible schema_version table.';
        END IF;
        BEGIN
            EXECUTE 'SELECT array_agg(version ORDER BY version) FROM parking.schema_version' INTO versions;
        EXCEPTION WHEN OTHERS THEN
            RAISE EXCEPTION 'Cannot read parking.schema_version; existing schema is incompatible.';
        END;
        IF versions IS DISTINCT FROM ARRAY[1] THEN
            RAISE EXCEPTION 'Expected parking.schema_version = 1; refusing migration.';
        END IF;
    ELSE
        CREATE SCHEMA parking;
    END IF;
    IF pg_catalog.pg_has_role(runtime_record.oid,
        (SELECT nspowner FROM pg_catalog.pg_namespace WHERE nspname = 'parking'), 'MEMBER') THEN
        RAISE EXCEPTION 'Runtime role must not own parking schema or inherit its owner role.';
    END IF;
END
$bootstrap$;

-- One set of definitions creates independent temporary reference tables for
-- catalog validation. Existing tables are never altered or replaced.
CREATE TEMP TABLE bootstrap_definitions (table_name text PRIMARY KEY, definition text NOT NULL) ON COMMIT DROP;
INSERT INTO bootstrap_definitions VALUES
('schema_version', $ddl$(
    version integer PRIMARY KEY CHECK (version = 1)
)$ddl$),
('admins', $ddl$(
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username text NOT NULL UNIQUE CHECK (username <> '' AND username = lower(btrim(username)) AND username !~ '^[[:space:]]|[[:space:]]$'),
    name text NOT NULL CHECK (name ~ '[^[:space:]]'),
    password_hash text NOT NULL CHECK (password_hash ~ '[^[:space:]]'),
    role text NOT NULL CHECK (role = 'admin')
)$ddl$),
('parking_spots', $ddl$(
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    location_name text NOT NULL CHECK (location_name ~ '[^[:space:]]'),
    address text NOT NULL CHECK (address ~ '[^[:space:]]'),
    motorcycle_rate numeric(12,2) NOT NULL CHECK (motorcycle_rate >= 0),
    car_rate numeric(12,2) NOT NULL CHECK (car_rate >= 0),
    capacity integer NOT NULL CHECK (capacity > 0),
    is_active boolean NOT NULL DEFAULT true
)$ddl$),
('jukir', $ddl$(
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parking_spot_id integer NOT NULL REFERENCES @schema@.parking_spots(id) ON DELETE RESTRICT,
    username text NOT NULL UNIQUE CHECK (username <> '' AND username = lower(btrim(username)) AND username !~ '^[[:space:]]|[[:space:]]$'),
    name text NOT NULL CHECK (name ~ '[^[:space:]]'),
    password_hash text NOT NULL CHECK (password_hash ~ '[^[:space:]]'),
    is_active boolean NOT NULL DEFAULT true
)$ddl$),
('parking_transactions', $ddl$(
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    parking_spot_id integer NOT NULL REFERENCES @schema@.parking_spots(id) ON DELETE RESTRICT,
    jukir_id integer NOT NULL REFERENCES @schema@.jukir(id) ON DELETE RESTRICT,
    plate_number text NOT NULL CHECK (char_length(plate_number) BETWEEN 1 AND 20 AND plate_number COLLATE "C" ~ '^[A-Z0-9]+$'),
    vehicle_type text NOT NULL CHECK (vehicle_type IN ('motorcycle', 'car')),
    entry_time timestamptz NOT NULL,
    exit_time timestamptz,
    rate_at_entry numeric(12,2) NOT NULL CHECK (rate_at_entry >= 0),
    fare numeric(12,2),
    status text NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'completed')),
    exit_jukir_id integer REFERENCES @schema@.jukir(id) ON DELETE RESTRICT,
    CHECK ((status = 'active' AND exit_time IS NULL AND fare IS NULL AND exit_jukir_id IS NULL)
        OR (status = 'completed' AND exit_time IS NOT NULL AND fare IS NOT NULL
            AND exit_jukir_id IS NOT NULL AND fare >= 0 AND exit_time >= entry_time))
)$ddl$),
('deposits', $ddl$(
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    jukir_id integer NOT NULL REFERENCES @schema@.jukir(id) ON DELETE RESTRICT,
    admin_id integer REFERENCES @schema@.admins(id) ON DELETE RESTRICT,
    deposit_date date NOT NULL,
    amount_deposited numeric(12,2) NOT NULL CHECK (amount_deposited >= 0),
    amount_expected numeric(12,2) NOT NULL CHECK (amount_expected >= 0),
    discrepancy numeric(12,2) GENERATED ALWAYS AS (amount_deposited - amount_expected) STORED,
    verification_status text NOT NULL DEFAULT 'pending' CHECK (verification_status IN ('pending', 'matched', 'need_review')),
    notes text,
    UNIQUE (jukir_id, deposit_date),
    CHECK ((verification_status = 'pending' AND admin_id IS NULL)
        OR (verification_status = 'matched' AND admin_id IS NOT NULL AND discrepancy = 0)
        OR (verification_status = 'need_review' AND admin_id IS NOT NULL AND discrepancy <> 0))
)$ddl$);

CREATE TEMP TABLE bootstrap_indexes (index_name text PRIMARY KEY, table_name text NOT NULL, definition text NOT NULL) ON COMMIT DROP;
INSERT INTO bootstrap_indexes VALUES
('parking_spots_location_name_key', 'parking_spots', 'UNIQUE INDEX parking_spots_location_name_key ON @schema@.parking_spots (lower(btrim(location_name)))'),
('parking_transactions_active_plate_key', 'parking_transactions', 'UNIQUE INDEX parking_transactions_active_plate_key ON @schema@.parking_transactions (plate_number) WHERE status = ''active'''),
('parking_transactions_location_status_idx', 'parking_transactions', 'INDEX parking_transactions_location_status_idx ON @schema@.parking_transactions (parking_spot_id, status)'),
('parking_transactions_exit_jukir_time_idx', 'parking_transactions', 'INDEX parking_transactions_exit_jukir_time_idx ON @schema@.parking_transactions (exit_jukir_id, exit_time)'),
('deposits_date_status_idx', 'deposits', 'INDEX deposits_date_status_idx ON @schema@.deposits (deposit_date, verification_status)');

DO $tables$
DECLARE
    item record;
    is_new boolean;
BEGIN
    is_new := to_regclass('parking.schema_version') IS NULL;
    -- FK dependency order, not alphabetic order.
    FOR item IN SELECT * FROM bootstrap_definitions
        ORDER BY array_position(ARRAY['schema_version','admins','parking_spots','jukir','parking_transactions','deposits'], table_name)
    LOOP
        EXECUTE format('CREATE TEMP TABLE %I %s ON COMMIT DROP', item.table_name,
            replace(item.definition, '@schema@', 'pg_temp'));
        IF is_new THEN
            EXECUTE format('CREATE TABLE parking.%I %s', item.table_name,
                replace(item.definition, '@schema@', 'parking'));
        END IF;
    END LOOP;
    FOR item IN SELECT * FROM bootstrap_indexes LOOP
        EXECUTE 'CREATE ' || replace(item.definition, '@schema@', 'pg_temp');
        IF is_new THEN
            EXECUTE 'CREATE ' || replace(item.definition, '@schema@', 'parking');
        END IF;
    END LOOP;
    IF is_new THEN
        INSERT INTO parking.schema_version VALUES (1);
    END IF;
END
$tables$;

-- Compare ordered column attributes/defaults, every constraint (including FK
-- actions/targets and validation), and every index's keys/predicate/expressions.
-- Reference objects live only in this connection's temporary schema.
DO $compatibility$
DECLARE
    item record;
    actual oid;
    expected oid;
    actual_shape jsonb;
    expected_shape jsonb;
    relation oid;
    shape jsonb;
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'parking' AND c.relkind IN ('r','p','v','m','f')
          AND c.relname NOT IN (SELECT table_name FROM bootstrap_definitions)
    ) THEN
        RAISE EXCEPTION 'Unexpected relation in parking schema; refusing incompatible schema.';
    END IF;
    FOR item IN SELECT table_name FROM bootstrap_definitions LOOP
        actual := to_regclass(format('parking.%I', item.table_name));
        expected := to_regclass(format('pg_temp.%I', item.table_name));
        IF actual IS NULL OR NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_class WHERE oid = actual AND relkind = 'r'
                AND relpersistence = 'p' AND NOT relrowsecurity AND NOT relforcerowsecurity
        ) THEN
            RAISE EXCEPTION 'Missing or incompatible table parking.%', item.table_name;
        END IF;
        actual_shape := NULL;
        FOREACH relation IN ARRAY ARRAY[actual, expected] LOOP
            SELECT jsonb_build_object(
                'columns', (SELECT jsonb_agg(jsonb_build_array(a.attname,
                    pg_catalog.format_type(a.atttypid, a.atttypmod), a.attnotnull,
                    a.attidentity, a.attgenerated, a.attcollation,
                    pg_catalog.pg_get_expr(d.adbin, d.adrelid)) ORDER BY a.attnum)
                    FROM pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef d
                        ON d.adrelid = a.attrelid AND d.adnum = a.attnum
                    WHERE a.attrelid = relation AND a.attnum > 0 AND NOT a.attisdropped),
                'constraints', (SELECT jsonb_agg(jsonb_build_array(c.conname, c.contype,
                    c.conkey, c.confkey, target.relname,
                    CASE WHEN c.confrelid = 0 THEN NULL ELSE target.relnamespace = (SELECT relnamespace FROM pg_catalog.pg_class WHERE oid = relation) END,
                    c.confupdtype, c.confdeltype,
                    c.confmatchtype, c.condeferrable, c.condeferred, c.convalidated,
                    pg_catalog.pg_get_expr(c.conbin, c.conrelid)) ORDER BY c.conname)
                    FROM pg_catalog.pg_constraint c LEFT JOIN pg_catalog.pg_class target ON target.oid = c.confrelid
                    WHERE c.conrelid = relation),
                'indexes', (SELECT jsonb_agg(jsonb_build_array(index_table.relname,
                    i.indisunique, i.indisprimary, i.indisvalid, i.indisready,
                    i.indnullsnotdistinct, index_table.relam,
                    i.indnkeyatts, i.indnatts, i.indkey::text, i.indclass::text,
                    i.indcollation::text, i.indoption::text,
                    pg_catalog.pg_get_expr(i.indexprs, i.indrelid),
                    pg_catalog.pg_get_expr(i.indpred, i.indrelid)) ORDER BY index_table.relname)
                    FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class index_table ON index_table.oid = i.indexrelid
                    WHERE i.indrelid = relation)
            ) INTO shape;
            IF actual_shape IS NULL THEN actual_shape := shape;
            ELSE expected_shape := shape;
            END IF;
        END LOOP;
        IF actual_shape IS DISTINCT FROM expected_shape THEN
            RAISE EXCEPTION 'Incompatible columns, constraints, or indexes in parking.% (schema version 1).', item.table_name;
        END IF;
        IF EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgrelid = actual AND NOT tgisinternal)
           OR EXISTS (SELECT 1 FROM pg_catalog.pg_rewrite WHERE ev_class = actual)
           OR EXISTS (SELECT 1 FROM pg_catalog.pg_inherits WHERE inhrelid = actual OR inhparent = actual) THEN
            RAISE EXCEPTION 'Unexpected trigger, rule, or inheritance in parking.%', item.table_name;
        END IF;
    END LOOP;
END
$compatibility$;

DO $permissions$
DECLARE
    runtime_name text := (SELECT runtime_role FROM bootstrap_options);
    item record;
BEGIN
    -- Runtime credentials intentionally cannot migrate/drop/truncate/delete.
    -- Direct DB credentials remain a trusted LAN boundary, not per-user auth.
    REVOKE ALL ON SCHEMA parking FROM PUBLIC;
    REVOKE ALL ON ALL TABLES IN SCHEMA parking FROM PUBLIC;
    REVOKE ALL ON ALL SEQUENCES IN SCHEMA parking FROM PUBLIC;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'parking' AND pg_catalog.pg_has_role(runtime_name, c.relowner, 'MEMBER')
    ) THEN
        RAISE EXCEPTION 'Runtime role must not own or inherit ownership of parking objects.';
    END IF;
    EXECUTE format('REVOKE ALL ON SCHEMA parking FROM %I', runtime_name);
    EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA parking FROM %I', runtime_name);
    EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA parking FROM %I', runtime_name);
    EXECUTE format('GRANT USAGE ON SCHEMA parking TO %I', runtime_name);
    EXECUTE format('GRANT SELECT ON parking.schema_version TO %I', runtime_name);
    FOR item IN SELECT table_name FROM bootstrap_definitions WHERE table_name <> 'schema_version' LOOP
        EXECUTE format('GRANT SELECT, INSERT, UPDATE ON parking.%I TO %I', item.table_name, runtime_name);
    END LOOP;
    EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA parking TO %I', runtime_name);
    IF pg_catalog.has_schema_privilege(runtime_name, 'parking', 'CREATE') THEN
        RAISE EXCEPTION 'Runtime role inherits schema CREATE; use a dedicated unprivileged role.';
    END IF;
    IF pg_catalog.has_table_privilege(runtime_name, 'parking.schema_version', 'INSERT')
       OR pg_catalog.has_table_privilege(runtime_name, 'parking.schema_version', 'UPDATE') THEN
        RAISE EXCEPTION 'Runtime role inherits schema_version write privileges; use a dedicated unprivileged role.';
    END IF;
    FOR item IN SELECT table_name FROM bootstrap_definitions LOOP
        IF pg_catalog.has_table_privilege(runtime_name, format('parking.%I', item.table_name), 'DELETE')
           OR pg_catalog.has_table_privilege(runtime_name, format('parking.%I', item.table_name), 'TRUNCATE')
           OR pg_catalog.has_table_privilege(runtime_name, format('parking.%I', item.table_name), 'TRIGGER')
           OR pg_catalog.has_table_privilege(runtime_name, format('parking.%I', item.table_name), 'REFERENCES') THEN
            RAISE EXCEPTION 'Runtime role inherits excessive table privileges; use a dedicated unprivileged role.';
        END IF;
    END LOOP;
END
$permissions$;
COMMIT;
