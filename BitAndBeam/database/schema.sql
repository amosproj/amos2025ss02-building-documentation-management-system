-- PostgreSQL Database Schema for BUILD.ING Document Management System

-- Users Table
CREATE TABLE "users" (
    "user_id" SERIAL PRIMARY KEY,
    "username" VARCHAR(50) UNIQUE NOT NULL,
    "email" VARCHAR(100) UNIQUE NOT NULL,
    "password_hash" VARCHAR(255) NOT NULL,
    "first_name" VARCHAR(50),
    "last_name" VARCHAR(50),
    "role" VARCHAR(20) CHECK (role IN ('admin', 'editor', 'viewer')) NOT NULL DEFAULT 'viewer',
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "last_login" TIMESTAMP WITH TIME ZONE,
    "is_active" BOOLEAN DEFAULT TRUE
);

-- Buildings Table
CREATE TABLE "buildings" (
    "building_id" SERIAL PRIMARY KEY,
    "name" VARCHAR(100) NOT NULL,
    "address" TEXT,
    "construction_year" INTEGER,
    "total_area" DECIMAL(10, 2),
    "floors" INTEGER,
    "description" TEXT,
    "coordinates" POINT,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Document Categories Table
CREATE TABLE "document_categories" (
    "category_id" SERIAL PRIMARY KEY,
    "name" VARCHAR(50) NOT NULL,
    "description" TEXT,
    "parent_category_id" INTEGER REFERENCES "document_categories" ("category_id") ON DELETE SET NULL,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Documents Table
CREATE TABLE "documents" (
    "document_id" SERIAL PRIMARY KEY,
    "title" VARCHAR(255) NOT NULL,
    "file_path" VARCHAR(255) NOT NULL,
    "file_type" VARCHAR(20) NOT NULL,
    "file_size" INTEGER NOT NULL, -- Size in bytes
    "category_id" INTEGER REFERENCES "document_categories" ("category_id") ON DELETE SET NULL,
    "building_id" INTEGER REFERENCES "buildings" ("building_id") ON DELETE CASCADE,
    "uploaded_by" INTEGER REFERENCES "users" ("user_id") ON DELETE SET NULL,
    "upload_date" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "last_modified" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "version" VARCHAR(20) DEFAULT '1.0',
    "status" VARCHAR(20) CHECK (status IN ('draft', 'review', 'approved', 'archived')) DEFAULT 'draft',
    "description" TEXT,
    "is_public" BOOLEAN DEFAULT FALSE,
    "metadata" JSONB -- Flexible metadata storage
);

-- Document Tags Table
CREATE TABLE "document_tags" (
    "tag_id" SERIAL PRIMARY KEY,
    "name" VARCHAR(50) NOT NULL UNIQUE,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Document-Tag Relationship (Many-to-Many)
CREATE TABLE "document_tag_relations" (
    "document_id" INTEGER REFERENCES "documents" ("document_id") ON DELETE CASCADE,
    "tag_id" INTEGER REFERENCES "document_tags" ("tag_id") ON DELETE CASCADE,
    PRIMARY KEY ("document_id", "tag_id")
);

-- Document Access Permissions (Many-to-Many)
CREATE TABLE "document_permissions" (
    "document_id" INTEGER REFERENCES "documents" ("document_id") ON DELETE CASCADE,
    "user_id" INTEGER REFERENCES "users" ("user_id") ON DELETE CASCADE,
    "permission_type" VARCHAR(20) CHECK (permission_type IN ('read', 'write', 'admin')) DEFAULT 'read',
    "granted_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "granted_by" INTEGER REFERENCES "users" ("user_id") ON DELETE SET NULL,
    PRIMARY KEY ("document_id", "user_id")
);

-- Document Version History
CREATE TABLE "document_versions" (
    "version_id" SERIAL PRIMARY KEY,
    "document_id" INTEGER REFERENCES "documents" ("document_id") ON DELETE CASCADE,
    "version_number" VARCHAR(20) NOT NULL,
    "file_path" VARCHAR(255) NOT NULL,
    "file_size" INTEGER NOT NULL,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "created_by" INTEGER REFERENCES "users" ("user_id") ON DELETE SET NULL,
    "change_summary" TEXT
);

-- Building-Document Relationship (Many-to-Many for documents related to multiple buildings)
CREATE TABLE "building_document_relations" (
    "building_id" INTEGER REFERENCES "buildings" ("building_id") ON DELETE CASCADE,
    "document_id" INTEGER REFERENCES "documents" ("document_id") ON DELETE CASCADE,
    "relation_type" VARCHAR(50), -- e.g., 'primary', 'reference', 'attachment'
    PRIMARY KEY ("building_id", "document_id")
);

-- Create indexes for performance
CREATE INDEX idx_documents_building_id ON documents(building_id);
CREATE INDEX idx_documents_category_id ON documents(category_id);
CREATE INDEX idx_documents_uploaded_by ON documents(uploaded_by);
CREATE INDEX idx_document_versions_document_id ON document_versions(document_id);
CREATE INDEX idx_document_tags_name ON document_tags(name);

-- Add trigger to update last_modified timestamp
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.last_modified = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_document_modtime
BEFORE UPDATE ON documents
FOR EACH ROW
EXECUTE FUNCTION update_modified_column();

CREATE TRIGGER update_building_modtime
BEFORE UPDATE ON buildings
FOR EACH ROW
EXECUTE FUNCTION update_modified_column();
