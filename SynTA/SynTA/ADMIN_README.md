# SynTA - Admin Area Documentation

## Overview
The Admin Area provides comprehensive administrative oversight and management capabilities for the SynTA application. Only users with the "Admin" role can access these features.

## Features

### Admin Dashboard
- **Location**: `/Admin/Dashboard/Index`
- **Features**:
  - Overview statistics (Total Users, Projects, User Stories, Gherkin Scenarios, Cypress Scripts)
  - New users this month
  - Active users this month
  - Top 5 users by activity
  - Recent activity feed (last 10 activities)

### User Management
- **Location**: `/Admin/Users/Index`
- **Features**:
  - View all registered users
  - User status (Active/Locked)
  - User roles (Admin/User)
  - Activity statistics per user
  - Last activity tracking

### User Details & Actions
- **Location**: `/Admin/Users/Details/{userId}`
- **Features**:
  - Detailed user information
  - Activity statistics
  - List of user's projects
  - **Admin Actions**:
    - Lock/Unlock user accounts
    - Grant/Remove admin privileges
    - Delete user accounts (with all associated data)

## Security

### Authorization
- All admin controllers are protected with the `[Authorize(Policy = "RequireAdminRole")]` attribute
- Only users with the "Admin" role can access admin features
- Admins cannot:
  - Lock their own account
  - Remove their own admin privileges
  - Delete their own account

### Role Management
Roles are managed using ASP.NET Core Identity:
- **Admin**: Full administrative access
- **User**: Standard user access

## Default Admin Account

A default admin account is automatically created when the application starts:

- **Email**: `admin@synta.com`
- **Password**: `Admin@123`

?? **IMPORTANT**: Change this password immediately in production!

## Accessing the Admin Area

1. Log in with an admin account
2. Click the "Admin" link in the navigation bar
3. You'll be redirected to the Admin Dashboard

## Database Migrations

If you're setting up the application for the first time, make sure to:

1. Update the database with the latest migrations:
   ```bash
   dotnet ef database update --project SynTA
   ```

2. The application will automatically seed the Admin and User roles on startup

## User Activity Tracking

The admin dashboard tracks:
- Project creation and updates
- User story creation
- Gherkin scenario generation
- Cypress script generation

This provides insights into how users are utilizing the application.

## Best Practices

1. **Regularly review user activity** to ensure the application is being used properly
2. **Monitor new user registrations** to detect any suspicious activity
3. **Lock accounts** that violate terms of service rather than immediately deleting them
4. **Keep at least one admin account active** to maintain administrative access
5. **Document any admin actions** taken for audit purposes

## Future Enhancements

Potential improvements for the admin area:
- Bulk user operations
- Export user and activity data
- Advanced search and filtering
- Email notifications to users
- Custom role creation
- Application settings management
- System health monitoring
- Audit log of all admin actions

## Support

For issues or questions about the admin area, please contact the development team or create an issue in the project repository.
