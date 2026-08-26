# Issue #1: Customer Self-Service Delivery Scheduling Portal

## Overview
Enable customers to independently schedule heating oil deliveries through the web application, reducing phone call volume and improving customer satisfaction while maintaining delivery efficiency and safety standards.

## Context and Background

### Business Context
Progress Home Heating currently requires customers to call the office to schedule deliveries or rely on automatic delivery based on system predictions. This creates:
- High call center workload during peak winter months
- Limited scheduling flexibility for customers outside business hours
- Inability for customers to see real-time driver availability
- Friction in the customer experience for simple scheduling tasks

### Current System Architecture
The application is built as a distributed .NET Aspire solution with:
- **ProgressHomeHeating.Web** - Blazor web UI using Telerik UI components
- **ProgressHomeHeating.OperationsApi** - REST API managing delivery orders, customers, tanks, and fleet
- **ProgressHomeHeating.AgentApi** - AI agent service for customer support (currently exists)
- **ProgressHomeHeating.Contracts** - Shared DTOs and domain models
- PostgreSQL database via Entity Framework Core

### Existing Capabilities
- Customer data management (Customer, ServiceAddress)
- Delivery order creation and management (DeliveryOrderDto, CreateDeliveryOrderRequest)
- Tank information tracking
- Fleet and driver management
- Pricing tiers and plans (Will-Call, Automatic Delivery, Budget Plan)

## Problem Statement
Customers currently cannot self-service their delivery scheduling needs, leading to:
- Customer frustration when unable to reach office during business hours
- Operational inefficiency from routine scheduling phone calls
- Missed revenue opportunities from customers who delay ordering
- Limited customer visibility into delivery slots and pricing

## User Stories

### US-001: Customer Views Available Delivery Dates
**As a** registered customer  
**I want to** view available delivery dates and time windows for my service address  
**So that** I can choose a convenient time that fits my schedule

**Acceptance Criteria:**
- AC-001.1: System displays a calendar view showing next 14 days
- AC-001.2: Unavailable dates are clearly marked and not selectable
- AC-001.3: Available time windows (AM/PM) are shown for each available date
- AC-001.4: System accounts for driver/truck capacity when showing availability
- AC-001.5: System displays any scheduling restrictions based on minimum order requirements (100 gallons)

### US-002: Customer Submits Delivery Order Request
**As a** registered customer  
**I want to** submit a delivery order request with my preferred date and gallons needed  
**So that** I can schedule oil delivery without calling the office

**Acceptance Criteria:**
- AC-002.1: Customer can select from their registered tank locations
- AC-002.2: System validates minimum gallon order (100 gallons per pricing policy)
- AC-002.3: System calculates and displays estimated cost based on pricing tier and current market price
- AC-002.4: Customer receives immediate confirmation with order number
- AC-002.5: System creates delivery order in pending status for office review
- AC-002.6: Confirmation includes estimated delivery window, gallons, and price

### US-003: Customer Views Current and Past Delivery Orders
**As a** registered customer  
**I want to** view my upcoming scheduled deliveries and past delivery history  
**So that** I can track my orders and plan accordingly

**Acceptance Criteria:**
- AC-003.1: Dashboard shows all scheduled deliveries with status (Pending, Scheduled, In Progress, Completed)
- AC-003.2: Past delivery history is accessible with filters (date range, status)
- AC-003.3: Each order displays: scheduled date, gallons requested/delivered, price, status
- AC-003.4: Customer can see assigned driver and truck information once scheduled
- AC-003.5: Real-time status updates are reflected without manual refresh

### US-004: Customer Modifies or Cancels Pending Delivery Order
**As a** registered customer  
**I want to** modify or cancel my pending delivery order  
**So that** I can adjust my schedule without calling the office

**Acceptance Criteria:**
- AC-004.1: Customer can cancel orders in "Pending" status up to 24 hours before scheduled date
- AC-004.2: Customer can modify date/time for "Pending" orders based on availability
- AC-004.3: System displays cancellation policy from knowledge base
- AC-004.4: Confirmation email sent for cancellations and modifications
- AC-004.5: Cannot modify/cancel orders in "Scheduled", "In Progress", or "Completed" status
- AC-004.6: System enforces cancellation deadline per policy

### US-005: Customer Authenticates to Access Scheduling Portal
**As a** customer  
**I want to** securely log in to the scheduling portal  
**So that** I can access my account and schedule deliveries

**Acceptance Criteria:**
- AC-005.1: Customer can log in using email and password
- AC-005.2: System validates customer identity against existing customer database
- AC-005.3: Customer session is maintained across pages
- AC-005.4: Unauthorized users cannot access scheduling functionality
- AC-005.5: Authentication state persists appropriately per web application security standards

### US-006: Customer Receives Dynamic Pricing Information
**As a** customer  
**I want to** see pricing information for my delivery request  
**So that** I can make informed decisions about delivery timing and volume

**Acceptance Criteria:**
- AC-006.1: System displays current market price for heating oil
- AC-006.2: Volume tier discounts are automatically calculated and shown:
  - Standard (100-199 gal): Market price
  - Volume (200-349 gal): Market price - $0.03/gal
  - Bulk (350+ gal): Market price - $0.06/gal
- AC-006.3: Automatic delivery service fee ($0.10/gal) is added if applicable to customer account
- AC-006.4: Emergency same-day delivery fee ($50) is shown if applicable
- AC-006.5: Total estimated cost is prominently displayed before order submission

## Technical Considerations

### Impacted Components
**Monorepo Services:**
- **ProgressHomeHeating.Web** - New Razor components for scheduling UI
- **ProgressHomeHeating.OperationsApi** - New endpoints for customer-facing scheduling
- **ProgressHomeHeating.Contracts** - May need new DTOs for scheduling-specific operations

### Dependencies
- **Existing Dependencies:**
  - Customer data (CustomerDto, ServiceAddressDto)
  - Tank information (TankContracts)
  - Delivery order system (DeliveryOrderDto, CreateDeliveryOrderRequest, UpdateDeliveryOrderRequest)
  - Fleet/driver availability data (FleetContracts)
  
- **New Dependencies:**
  - Authentication/authorization mechanism for customer portal
  - Real-time availability calculation based on fleet capacity
  - Pricing calculation service integrating knowledge base pricing rules

### Integration Points
- PostgreSQL database for reading customer, tank, and fleet data
- Existing OperationsApi endpoints may need extension or new endpoints
- Telerik Blazor components for UI consistency with existing application
- Potential integration with AgentApi for intelligent scheduling suggestions

### Security Considerations
- Customer authentication required to prevent unauthorized order creation
- Authorization to ensure customers only access their own orders
- Input validation on all delivery requests (gallons, dates, tank IDs)
- Rate limiting to prevent abuse of scheduling system
- Secure storage of customer credentials if new authentication implemented

## Potential Edge Cases and Risks

### Edge Cases
1. **Capacity Overflow**: Multiple customers scheduling same date/time exhausting fleet capacity
2. **Tank Capacity Validation**: Customer requests gallons exceeding their tank's remaining capacity
3. **Service Area Boundaries**: Customer address outside current service coverage area
4. **Concurrent Modifications**: Two customers requesting last available slot simultaneously
5. **Partial Availability**: Trucks available but no drivers, or vice versa
6. **Holiday/Emergency Scheduling**: Special handling during winter storms or company holidays
7. **Past Date Selections**: User attempts to schedule delivery for past dates
8. **Multiple Tank Scenarios**: Customers with multiple tanks at different addresses
9. **Budget Plan Customers**: Integration with existing automatic delivery commitments
10. **Price Volatility**: Market price changes between quote and order submission

### Risks
- **Operational Risk**: Self-service scheduling conflicts with existing automatic delivery system
- **Customer Expectation Risk**: Customers may expect immediate confirmation when office review is needed
- **Revenue Risk**: Customers may delay purchasing waiting for lower market prices
- **Data Integrity Risk**: Concurrent order creation may cause double-booking
- **UX Risk**: Complex pricing rules may confuse customers without clear explanation
- **Scope Creep Risk**: Feature may expand to include payment processing, subscription management
- **Migration Risk**: Existing automatic delivery customers need clear communication about new option

## Non-Functional Requirements (NFR)

### Performance (NFR-PERF)
- NFR-PERF-001: Availability calendar loads within 2 seconds
- NFR-PERF-002: Order submission completes within 3 seconds
- NFR-PERF-003: System supports concurrent scheduling by at least 50 customers
- NFR-PERF-004: Real-time availability updates reflect within 5 seconds of fleet changes

### Usability (NFR-USABILITY)
- NFR-USABILITY-001: Scheduling workflow completable in maximum 5 clicks
- NFR-USABILITY-002: Mobile-responsive design for smartphone and tablet access
- NFR-USABILITY-003: Clear error messages for validation failures with remediation guidance
- NFR-USABILITY-004: Pricing breakdown visible before final submission
- NFR-USABILITY-005: Accessibility compliance with WCAG 2.1 Level AA standards

### Reliability (NFR-RELIABILITY)
- NFR-RELIABILITY-001: Order submissions are atomic - either fully succeed or fully fail
- NFR-RELIABILITY-002: System gracefully handles database connectivity issues
- NFR-RELIABILITY-003: Customer receives confirmation regardless of email delivery success
- NFR-RELIABILITY-004: Pending orders persist even if application restarts

### Security (NFR-SECURITY)
- NFR-SECURITY-001: All customer data transmitted over HTTPS
- NFR-SECURITY-002: Authentication tokens expire after 24 hours of inactivity
- NFR-SECURITY-003: Failed login attempts are rate-limited (5 attempts per 15 minutes)
- NFR-SECURITY-004: Customer can only view/modify their own orders (authorization enforced)
- NFR-SECURITY-005: Input validation prevents SQL injection and XSS attacks

### Maintainability (NFR-MAINTAINABILITY)
- NFR-MAINTAINABILITY-001: New scheduling code follows existing project structure and patterns
- NFR-MAINTAINABILITY-002: API endpoints follow RESTful conventions consistent with OperationsApi
- NFR-MAINTAINABILITY-003: Pricing logic centralized for easy updates as policies change
- NFR-MAINTAINABILITY-004: Logging implemented for troubleshooting scheduling issues

### Compatibility (NFR-COMPATIBILITY)
- NFR-COMPATIBILITY-001: Works on latest 2 versions of Chrome, Firefox, Edge, Safari
- NFR-COMPATIBILITY-002: Mobile support for iOS 15+ and Android 11+
- NFR-COMPATIBILITY-003: Integrates with existing Aspire telemetry and monitoring

## Out of Scope (Explicitly Excluded)
- Payment processing and online payment collection
- SMS notifications for delivery updates
- Customer registration/account creation workflow (assumes customers already exist)
- Modification of automatic delivery prediction algorithms
- Mobile native applications (iOS/Android apps)
- Integration with external route optimization software
- Real-time driver GPS tracking for customers
- Customer reviews or ratings of drivers/service

## Success Metrics
- **Adoption Rate**: 30% of eligible customers schedule at least one delivery within first 3 months
- **Call Volume Reduction**: 40% reduction in scheduling-related phone calls
- **Customer Satisfaction**: NPS score improvement of 10+ points for scheduling experience
- **Order Completion Rate**: 85%+ of self-service orders convert to completed deliveries
- **Time to Schedule**: Average scheduling time under 3 minutes from login to confirmation

## Dependencies on Other Teams/Systems
- Operations team review and approval workflow for pending orders (may remain manual initially)
- Current market pricing data source (may be manual entry or external API)
- Email notification system for order confirmations
- Existing customer authentication system or decision on new authentication approach

## Questions for Stakeholders
*Before development begins, these areas need clarification:*

1. **Authentication**: Do we have an existing customer authentication system, or does this require implementing new customer login functionality?

2. **Order Approval**: Should customer orders be immediately confirmed as "Scheduled", or do they need office review/approval first?

3. **Pricing**: Is the market price stored in the database and manually updated, or is there an external pricing service?

4. **Automatic Delivery Customers**: Should customers on automatic delivery plans be allowed to schedule additional will-call orders, or are they restricted?

5. **Notification Requirements**: Beyond confirmation emails, are there other notifications needed (SMS, phone calls for high-value orders)?

6. **Emergency Deliveries**: Should same-day/emergency scheduling be available through self-service, or remain phone-only?

7. **Tank Level Data**: Do we have IoT tank level sensors, or does customer need to estimate their current tank level?

8. **Business Hours**: Should scheduling be available 24/7, or restricted to business hours?

---

**Issue Type**: Feature Enhancement  
**Priority**: High  
**Estimated Complexity**: Large  
**Target User**: Residential heating oil customers  
**Business Value**: Improved customer experience, reduced operational costs, competitive differentiation
