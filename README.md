# Donation Management System

A **secure, transparent, and role-based platform** for managing charitable contributions across multiple causes.  
Developed with **ASP.NET Core Razor Pages** and **SQL Server**, this system empowers **Admins, Donors, and Volunteers** to collaborate effectively while ensuring accountability and transparency.

---

## 📌 Overview
The Donation Management System is designed to simplify and enhance charitable operations by focusing on:

- **Trust & Transparency**: Real-time fundraiser totals, dashboards, audit logs.  
- **Donor Engagement**: Tiered donation packages, digital receipts, project reviews, and history tracking.  
- **Operational Efficiency**: Role-specific workflows, assignment tracking, reporting, and streamlined project management.  

Supported segments include:  
Orphanage Support • Water in Africa • Gaza Relief • Old Age Home Care • Zakat • Eid/Ramadan

---

## ⚙️ Technology Stack
- **Framework**: ASP.NET Core Razor Pages  
- **Database**: Microsoft SQL Server  
- **Authentication & Security**: ASP.NET Core Identity with role-based access control  
- **UI Layer**: Razor Pages (Bootstrap/Tailwind optional)  
- **Reporting**: SQL Server Views integrated with dashboards  
- **Security Features**: Password hashing, anonymity support, audit logging  

---

## 👥 User Roles & Key Features

### 🔑 Admin
- Manage users (Admins, Donors, Volunteers).  
- Create, update, and deactivate **Projects** and **Fundraisers**.  
- Apply **advanced filters/search** (priority, category, date range, location).  
- Monitor dashboards:
  - Fundraiser live totals  
  - Segment performance (% target achieved)  
- Publish **Works of Organization** for impact storytelling.  
- Enforce transparency via **10% operational cost** reporting.  
- Maintain accountability through a comprehensive **AuditLog**.  

### 💵 Donor
- Browse causes and securely contribute to specific projects/fundraisers.  
- **Donation Packages with recognition**:
  - Premium (≥ $5M) → TV broadcast + site highlight  
  - Executive (≥ $1M) → Newspaper + site highlight  
  - Economy (≥ $100K) → Website recognition  
  - Standard (< $100K) → Normal listing  
- Post-login **donation summary popup**: total lifetime donations or “Please donate!” prompt.  
- **Anonymous donation** option for privacy.  
- Access **donation history**, filterable by event, time, or status.  
- **Download receipts** with unique ReceiptCode (PDF ready).  
- Submit **one review per project** (rating + comment).  

### 🙋 Volunteer
- Maintain a **profile with availability and skills**.  
- Receive and manage **assignments** with tracked hours and statuses.  
- Access **project/task list** and assignment history.  
- Receive **emergency alerts** from admins.  
- Communicate via **threaded in-app messaging**.  
- View contributions through **Works of Organization**.  

### 🌍 Transparency & Trust
- Fundraiser live totals updated in real time.  
- Segment dashboards displaying collected vs. target amounts.  
- Dedicated **Operational Cost Page**: 10% overhead displayed clearly.  
- Published impact stories with photos, videos, and proof of fieldwork.  
- **Audit logs** for every admin action.  

---

## 🗂️ Data Model Highlights
- **Entities**: `Admin`, `Donor`, `Volunteer`, `DonationCategory`, `Project`, `Fundraiser`, `Donation`, `Payment`, `Review`, `VolunteerAssignment`, `WorksOfOrganization`, `AuditLog`, `Financelog`.  
- **Enhancements**:
  - `Project.IsActive`, `Project.Location`  
  - `Donation.IsAnonymous`  
  - `Payment.ReceiptCode` (unique)  
  - Optional: `Donor.IsActive`, `Volunteer.IsActive` 
---

## 🔄 End-to-End Workflow

**Admin**  
1. Configure Segments → Projects → Fundraisers.  
2. Assign Volunteers.  
3. Publish Works of Organization.  

**Donor**  
1. Browse fundraisers, select donation package.  
2. Complete payment → receipt generated.  
3. Recognition applied automatically.  
4. Optionally submit review.  

**Volunteer**  
1. Update profile/availability.  
2. Receive project assignments.  
3. Log hours and track completion.  
4. Receive emergency alerts/messages.  

**Transparency**  
- Real-time dashboards for all users.  
- Operational costs fixed at 10%.  
- Works of Organization linked to funded projects.  

---

## ✅ Acceptance Criteria
- Donor login popup shows lifetime total; $0 triggers “Please donate!” message.  
- Donation packages enforce tier-based recognition.  
- Receipts generated with unique codes and available for download.  
- Operational cost page always shows 10% overhead.  
- Admins can toggle project activation.  
- Dashboards update automatically via SQL views.  
- Anonymous donations conceal donor identity.  
- All admin actions logged in `AuditLog`.  

---

## 📦 Installation

### 1. Clone Repository
```bash
gh repo clone sarartaher/C-Project-Donation_Website
