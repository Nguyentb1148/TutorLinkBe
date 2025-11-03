using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorLinkBe.Migrations
{
    public partial class FixEnrollmentStatusConversion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe conversion to text: create temporary text column, copy data, drop old column, rename temp.
            // If your existing column already is text this will still run safely.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name='classroomstudents' AND column_name='enrollmentstatus'
                  ) THEN
                    -- add temp text column if missing
                    IF NOT EXISTS (
                      SELECT 1 FROM information_schema.columns
                      WHERE table_name='classroomstudents' AND column_name='enrollmentstatus_tmp'
                    ) THEN
                      ALTER TABLE ""ClassroomStudents"" ADD COLUMN ""EnrollmentStatus_tmp"" text;
                    END IF;

                    -- copy values safely. If EnrollmentStatus is integer, this casts integer->text.
                    -- If you need to map integer enum values to names, replace the UPDATE below with a CASE expression:
                    -- UPDATE ""ClassroomStudents"" SET ""EnrollmentStatus_tmp"" =
                    --   CASE ""EnrollmentStatus""
                    --     WHEN 0 THEN 'Pending'
                    --     WHEN 1 THEN 'Approved'
                    --     WHEN 2 THEN 'Rejected'
                    --     ELSE 'Pending' END;
                    UPDATE ""ClassroomStudents"" SET ""EnrollmentStatus_tmp"" = ""EnrollmentStatus""::text;

                    -- drop old column and rename temp
                    ALTER TABLE ""ClassroomStudents"" DROP COLUMN ""EnrollmentStatus"";
                    ALTER TABLE ""ClassroomStudents"" RENAME COLUMN ""EnrollmentStatus_tmp"" TO ""EnrollmentStatus"";

                    -- set default and not null to match EF mapping
                    ALTER TABLE ""ClassroomStudents"" ALTER COLUMN ""EnrollmentStatus"" SET DEFAULT 'Pending';
                    ALTER TABLE ""ClassroomStudents"" ALTER COLUMN ""EnrollmentStatus"" SET NOT NULL;
                  END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op rollback. Implement if you need to revert back to integer mapping.
            migrationBuilder.Sql(@"
                -- Down migration intentionally left empty. If you need to revert to integer, implement mapping here.
                SELECT 1;
            ");
        }
    }
}
