/*
  # Create Users and Projects Schema

  1. New Tables
    - `users` - Store application users
    - `projects` - Store generated projects
    - `entities` - Store entity definitions for projects
    - `entity_properties` - Store properties for entities
    - `entity_relations` - Store relationships between entities
    - `generated_queries` - Store saved queries
    
  2. Security
    - Enable RLS on all tables
    - Users can only access their own projects
    - Proper ownership checks
    
  3. Features
    - Soft delete support
    - Timestamps for audit trail
    - Project status tracking
    - Code generation tracking
*/

-- Create users table
CREATE TABLE IF NOT EXISTS users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email TEXT UNIQUE NOT NULL,
  username TEXT UNIQUE NOT NULL,
  full_name TEXT,
  password_hash TEXT NOT NULL,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

-- Create projects table
CREATE TABLE IF NOT EXISTS projects (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  description TEXT,
  database_type TEXT NOT NULL DEFAULT 'SqlServer',
  backend_framework TEXT NOT NULL DEFAULT 'DotNet10',
  architecture_style TEXT NOT NULL DEFAULT 'CleanArchitecture',
  status TEXT NOT NULL DEFAULT 'Draft',
  code_generated_at TIMESTAMPTZ,
  last_modified TIMESTAMPTZ DEFAULT now(),
  created_at TIMESTAMPTZ DEFAULT now(),
  is_deleted BOOLEAN DEFAULT false,
  UNIQUE(user_id, name)
);

-- Create entities table
CREATE TABLE IF NOT EXISTS entities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  plural_name TEXT,
  description TEXT,
  is_deleted BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now(),
  UNIQUE(project_id, name)
);

-- Create entity properties table
CREATE TABLE IF NOT EXISTS entity_properties (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_id UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  data_type TEXT NOT NULL,
  is_nullable BOOLEAN DEFAULT false,
  is_primary_key BOOLEAN DEFAULT false,
  is_required BOOLEAN DEFAULT false,
  max_length INTEGER,
  annotations TEXT DEFAULT '[]',
  display_order INTEGER DEFAULT 0,
  created_at TIMESTAMPTZ DEFAULT now(),
  UNIQUE(entity_id, name)
);

-- Create entity relations table
CREATE TABLE IF NOT EXISTS entity_relations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  source_entity_id UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
  target_entity_id UUID NOT NULL REFERENCES entities(id) ON DELETE CASCADE,
  relation_type TEXT NOT NULL,
  foreign_key_name TEXT,
  navigation_property TEXT NOT NULL,
  inverse_navigation_property TEXT,
  join_table_name TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

-- Create generated queries table
CREATE TABLE IF NOT EXISTS generated_queries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  description TEXT,
  query_sql TEXT NOT NULL,
  source_entity TEXT NOT NULL,
  join_entities TEXT DEFAULT '[]',
  filters TEXT DEFAULT '[]',
  generated_dto TEXT,
  generated_repository TEXT,
  generated_service TEXT,
  generated_controller TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now(),
  UNIQUE(project_id, name)
);

-- Enable RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE entities ENABLE ROW LEVEL SECURITY;
ALTER TABLE entity_properties ENABLE ROW LEVEL SECURITY;
ALTER TABLE entity_relations ENABLE ROW LEVEL SECURITY;
ALTER TABLE generated_queries ENABLE ROW LEVEL SECURITY;

-- Create RLS Policies for users
CREATE POLICY "Users can read own profile"
  ON users FOR SELECT
  USING (auth.uid()::text = id::text);

-- Create RLS Policies for projects
CREATE POLICY "Users can view own projects"
  ON projects FOR SELECT
  USING (user_id = auth.uid());

CREATE POLICY "Users can create projects"
  ON projects FOR INSERT
  WITH CHECK (user_id = auth.uid());

CREATE POLICY "Users can update own projects"
  ON projects FOR UPDATE
  USING (user_id = auth.uid())
  WITH CHECK (user_id = auth.uid());

CREATE POLICY "Users can delete own projects"
  ON projects FOR DELETE
  USING (user_id = auth.uid());

-- Create RLS Policies for entities
CREATE POLICY "Users can view entities of their projects"
  ON entities FOR SELECT
  USING (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = entities.project_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can create entities in their projects"
  ON entities FOR INSERT
  WITH CHECK (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = entities.project_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can update entities in their projects"
  ON entities FOR UPDATE
  USING (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = entities.project_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can delete entities in their projects"
  ON entities FOR DELETE
  USING (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = entities.project_id
    AND projects.user_id = auth.uid()
  ));

-- Create RLS Policies for entity_properties
CREATE POLICY "Users can view properties of their entities"
  ON entity_properties FOR SELECT
  USING (EXISTS (
    SELECT 1 FROM entities
    JOIN projects ON projects.id = entities.project_id
    WHERE entities.id = entity_properties.entity_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can manage properties in their projects"
  ON entity_properties FOR INSERT
  WITH CHECK (EXISTS (
    SELECT 1 FROM entities
    JOIN projects ON projects.id = entities.project_id
    WHERE entities.id = entity_properties.entity_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can update properties in their projects"
  ON entity_properties FOR UPDATE
  USING (EXISTS (
    SELECT 1 FROM entities
    JOIN projects ON projects.id = entities.project_id
    WHERE entities.id = entity_properties.entity_id
    AND projects.user_id = auth.uid()
  ));

-- Create RLS Policies for entity_relations
CREATE POLICY "Users can view relations of their entities"
  ON entity_relations FOR SELECT
  USING (EXISTS (
    SELECT 1 FROM entities
    JOIN projects ON projects.id = entities.project_id
    WHERE entities.id = entity_relations.source_entity_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can manage relations in their projects"
  ON entity_relations FOR INSERT
  WITH CHECK (EXISTS (
    SELECT 1 FROM entities
    JOIN projects ON projects.id = entities.project_id
    WHERE entities.id = entity_relations.source_entity_id
    AND projects.user_id = auth.uid()
  ));

-- Create RLS Policies for generated_queries
CREATE POLICY "Users can view queries in their projects"
  ON generated_queries FOR SELECT
  USING (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = generated_queries.project_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can manage queries in their projects"
  ON generated_queries FOR INSERT
  WITH CHECK (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = generated_queries.project_id
    AND projects.user_id = auth.uid()
  ));

CREATE POLICY "Users can update queries in their projects"
  ON generated_queries FOR UPDATE
  USING (EXISTS (
    SELECT 1 FROM projects
    WHERE projects.id = generated_queries.project_id
    AND projects.user_id = auth.uid()
  ));

-- Create indexes for better performance
CREATE INDEX idx_projects_user_id ON projects(user_id);
CREATE INDEX idx_entities_project_id ON entities(project_id);
CREATE INDEX idx_entity_properties_entity_id ON entity_properties(entity_id);
CREATE INDEX idx_entity_relations_source ON entity_relations(source_entity_id);
CREATE INDEX idx_entity_relations_target ON entity_relations(target_entity_id);
CREATE INDEX idx_generated_queries_project_id ON generated_queries(project_id);
