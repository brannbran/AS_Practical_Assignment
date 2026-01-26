# ?? Email OTP 2FA Registration Guide

## ? **Implementation Complete**

Your registration process now requires **email verification with a 6-digit OTP** before account creation.

---

## ?? **How It Works**

### **Step 1: User Fills Registration Form**
- User provides: Name, Email, Password, NRIC, DOB, Resume, etc.
- reCAPTCHA v3 validates the user is human
- Form data is temporarily stored (encrypted in database)

### **Step 2: OTP Email Sent**
- System generates a cryptographically secure 6-digit code
- Code is stored in database with 10-minute expiration
- Professional email sent to user with the verification code

### **Step 3: User Verifies Email**
- User redirected to `/VerifyEmail` page
- Enters the 6-digit code from email
- Real-time countdown shows remaining time (10 minutes)
- Auto-submit when 6 digits are entered

### **Step 4: Account Created**
- Upon successful verification:
  - Account is created with verified email
  - User is automatically signed in
  - Welcome email is sent
  - Audit logs are created

---

## ?? **Security Features**

| Feature | Description |
|---------|-------------|
| **Cryptographically Secure OTP** | Generated using `RandomNumberGenerator` |
| **Time-Limited** | Valid for 10 minutes only |
| **Single-Use** | Marked as used after successful verification |
| **Auto-Invalidation** | Old OTPs invalidated when new one requested |
| **Resume Handling** | Temporarily stored as byte array, saved only after verification |
| **Audit Logging** | All OTP actions logged (sent, verified, failed) |
| **Email Verification** | Email confirmed automatically upon OTP verification |
| **Resend Protection** | 30-second cooldown on resend button |

---

## ?? **User Experience**

### **Registration Page (`/Register`)**
- **Form Fields:** First Name, Last Name, Gender, NRIC, Email, Password, DOB, Resume, Who Am I
- **reCAPTCHA v3:** Invisible bot protection
- **Button:** "Send Verification Code"
- **Info Alert:** Explains 3-step verification process

### **Verification Page (`/VerifyEmail`)**
- **Large OTP Input:** 6-digit code entry with auto-submit
- **Countdown Timer:** Visual progress bar + time remaining
- **Resend Option:** Available after 30 seconds
- **Security Tips:** Listed for user awareness
- **Auto-Focus:** OTP input automatically focused
- **Letter Spacing:** Large, spaced digits for easy reading

---

## ?? **Email Templates**

### **1. OTP Verification Email**
**Subject:** "Your Verification Code - Ace Job Agency"

**Content:**
- Professional header with company branding
- Large, centered 6-digit code with visual box
- Validity period (10 minutes)
- Security warnings (don't share code, etc.)
- Professional footer

### **2. Welcome Email** (Sent after successful registration)
**Subject:** "Welcome to Ace Job Agency!"

**Content:**
- Welcome message with user's first name
- List of features available
- Encouragement to explore opportunities
- Professional footer

---

## ?? **Configuration**

### **appsettings.json**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@acejobagency.com",
    "SenderName": "Ace Job Agency",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  }
}
```

### **For Gmail (Recommended for Testing)**

1. **Enable 2-Step Verification** on your Google account
2. **Generate App Password:**
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Other (Custom name)"
   - Copy the generated 16-character password
   - Use this as your `Password` in `appsettings.json`

3. **Update Configuration:**
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Ace Job Agency",
    "Username": "your-email@gmail.com",
    "Password": "YOUR_16_CHAR_APP_PASSWORD",
    "EnableSsl": true
  }
}
```

### **For Other Email Providers**

| Provider | SMTP Server | Port | SSL |
|----------|-------------|------|-----|
| **Gmail** | smtp.gmail.com | 587 | Yes |
| **Outlook** | smtp-mail.outlook.com | 587 | Yes |
| **Yahoo** | smtp.mail.yahoo.com | 587 | Yes |
| **SendGrid** | smtp.sendgrid.net | 587 | Yes |

---

## ??? **Database Schema**

### **EmailOtpTokens Table**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | Primary key |
| `Email` | nvarchar(450) | User's email (indexed) |
| `OtpCode` | nvarchar(6) | 6-digit verification code |
| `CreatedDate` | datetime2 | When OTP was generated |
| `ExpiryDate` | datetime2 | When OTP expires (10 min from creation) |
| `IsUsed` | bit | Whether OTP has been used |
| `IpAddress` | nvarchar(max) | IP address of requester |
| `RegistrationDataJson` | nvarchar(max) | Temporary registration data (JSON) |

**Indexes:**
- `Email` - For fast lookups
- `CreatedDate` - For cleanup queries
- `ExpiryDate` - For expiration checks

---

## ?? **Registration Flow Diagram**

```
???????????????????????????????????????????????????????????????
?       1. User Fills Form    ?
?     (/Register - First Name, Email, Password, etc.)    ?
???????????????????????????????????????????????????????????????
                 ?
  ?
???????????????????????????????????????????????????????????????
?      2. reCAPTCHA v3 Validation?
?        (Bot Detection)          ?
???????????????????????????????????????????????????????????????
               ?
         ?
???????????????????????????????????????????????????????????????
?       3. Generate 6-Digit OTP (10 min expiry)        ?
?      Store registration data temporarily in database        ?
???????????????????????????????????????????????????????????????
?
            ?
???????????????????????????????????????????????????????????????
?       4. Send OTP Email to User?
?  (Professional HTML email with verification code)      ?
???????????????????????????????????????????????????????????????
         ?
     ?
???????????????????????????????????????????????????????????????
?        5. Redirect to /VerifyEmail?email=xxx          ?
?     (User sees countdown timer and OTP input field)         ?
???????????????????????????????????????????????????????????????
           ?
          ?
???????????????????????????????????????????????????????????????
?   6. User Enters 6-Digit Code   ?
?         (Auto-submit when 6 digits entered)            ?
???????????????????????????????????????????????????????????????
          ?
          ?
???????????????????????????????????????????????????????????????
?              7. Validate OTP Code                   ?
?    (Check if valid, not used, and not expired)     ?
???????????????????????????????????????????????????????????????
      ?
              ???????????????????
                  ? ?
           Valid?   Invalid?
             ?  ?
      ?         ?
????????????????????????   ???????????????????
        ?  8. Create Account   ?   ?  Show Error     ?
        ?  - Encrypt data      ?   ?  - Allow retry  ?
        ?  - Save resume    ?   ?  - Resend option?
        ?  - Mark OTP used?   ???????????????????
        ?  - Send welcome email?
        ?  - Sign in user      ?
        ?  - Create session    ?
      ????????????????????????
        ?
           ?
        ????????????????????????
     ?  9. Redirect to /Index?
        ?   (User logged in) ?
      ????????????????????????
```

---

## ?? **Testing the OTP System**

### **Test Case 1: Successful Registration**

1. **Fill registration form** with valid data
2. **Click** "Send Verification Code"
3. **Check email** for 6-digit code
4. **Enter code** on verification page
5. **Verify:**
   - Account created successfully
   - Email marked as verified
   - User automatically logged in
   - Welcome email received

**Expected Result:** ? Success

---

### **Test Case 2: Expired OTP**

1. **Register and receive OTP**
2. **Wait 10+ minutes** (or modify code to 1 minute for testing)
3. **Try to verify** with the code

**Expected Result:** ? "Invalid or expired verification code"

---

### **Test Case 3: Invalid OTP**

1. **Register and receive OTP**
2. **Enter wrong code** (e.g., 123456 instead of actual code)

**Expected Result:** ? "Invalid or expired verification code"

---

### **Test Case 4: Resend OTP**

1. **Register and receive OTP**
2. **Click "Resend Code"** button
3. **Check email** for new code
4. **Verify** old code doesn't work
5. **Use new code** to complete registration

**Expected Result:** ? New code works, old code invalid

---

### **Test Case 5: Already Registered Email**

1. **Try to register** with an email that's already in the system

**Expected Result:** ? "This email address is already registered"

---

## ?? **Audit Log Entries**

All OTP-related activities are logged:

| Action | Status | Description |
|--------|--------|-------------|
| `OTP Sent` | Success | Email verification code sent |
| `OTP Verification Failed` | Failed | Invalid or expired OTP code |
| `OTP Verified & Registered` | Success | User verified email and completed registration |
| `OTP Resent` | Success | New OTP code generated and sent |
| `OTP Email Failed` | Failed | Failed to send OTP verification email |

---

## ??? **Security Best Practices Implemented**

### **1. Time-Limited Codes**
- OTPs expire after 10 minutes
- Prevents brute-force attacks over extended periods

### **2. Single-Use Codes**
- Each OTP can only be used once
- Prevents replay attacks

### **3. Automatic Invalidation**
- Requesting new OTP invalidates all previous ones
- Prevents confusion and potential security issues

### **4. Secure Generation**
- Uses `RandomNumberGenerator.Create()` (cryptographically secure)
- Not predictable like `Random()`

### **5. Rate Limiting**
- 30-second cooldown on resend button (client-side)
- Server-side can add additional rate limiting if needed

### **6. Data Encryption**
- Registration data stored temporarily in database
- Sensitive fields (NRIC, DOB, Name, WhoAmI) encrypted before final storage

### **7. Comprehensive Audit Trail**
- All OTP actions logged with IP address and user agent
- Enables security monitoring and compliance

---

## ?? **Production Deployment Checklist**

- [ ] **Email Service Configured**
  - [ ] SMTP credentials set in `appsettings.json`
  - [ ] Sender email verified with provider
  - [ ] Test email sending works

- [ ] **Database Migration Applied**
  - [ ] `EmailOtpTokens` table created
  - [ ] Indexes configured

- [ ] **Cleanup Task Scheduled**
  - [ ] Implement periodic cleanup of expired OTPs
  - [ ] Recommended: Daily cleanup job

- [ ] **Email Templates Reviewed**
  - [ ] Company branding added
  - [ ] Links tested
  - [ ] Mobile-responsive

- [ ] **Security Settings**
  - [ ] OTP expiry time reviewed (10 minutes recommended)
  - [ ] Rate limiting configured
  - [ ] Audit logging enabled

- [ ] **User Communication**
  - [ ] Registration instructions clear
  - [ ] Help documentation available
  - [ ] Support contact provided

---

## ?? **Maintenance**

### **Cleanup Expired OTPs**

Add this to a scheduled background task:

```csharp
// In a background service or scheduled task
await _emailOtpService.CleanupExpiredOtpsAsync();
```

**Recommendation:** Run daily at off-peak hours

### **Monitor Email Delivery**

- Check audit logs for failed email sends
- Monitor email provider's delivery reports
- Set up alerts for high failure rates

### **Update Email Templates**

Email templates are in `Services/EmailService.cs`:
- `SendOtpEmailAsync()` - OTP verification email
- `SendWelcomeEmailAsync()` - Welcome email

---

## ?? **Statistics & Monitoring**

### **Queries for Monitoring**

**OTP Success Rate:**
```sql
SELECT 
    COUNT(CASE WHEN IsUsed = 1 THEN 1 END) as Successful,
    COUNT(CASE WHEN IsUsed = 0 AND ExpiryDate < GETUTCDATE() THEN 1 END) as Expired,
    COUNT(*) as Total
FROM EmailOtpTokens
WHERE CreatedDate >= DATEADD(DAY, -7, GETUTCDATE())
```

**Average Verification Time:**
```sql
SELECT 
    AVG(DATEDIFF(SECOND, o.CreatedDate, a.Timestamp)) as AvgSeconds
FROM EmailOtpTokens o
INNER JOIN AuditLogs a ON a.UserEmail = o.Email 
    AND a.Action = 'OTP Verified & Registered'
WHERE o.IsUsed = 1
```

---

## ?? **Benefits**

1. **Enhanced Security:** Email verification proves user owns the email
2. **Reduced Spam:** Fake registrations significantly reduced
3. **Better User Quality:** Only real users with valid emails
4. **Compliance:** Meets requirements for verified communication channels
5. **Audit Trail:** Complete record of registration attempts
6. **Professional Image:** High-quality email templates improve brand perception

---

## ? **Troubleshooting**

### **Issue: Emails Not Sending**

**Check:**
1. SMTP credentials in `appsettings.json`
2. Firewall rules (port 587 outbound)
3. Email provider's security settings
4. Check audit logs for specific error messages

**Solution:**
```sql
SELECT TOP 10 *
FROM AuditLogs
WHERE Action = 'OTP Email Failed'
ORDER BY Timestamp DESC
```

### **Issue: OTP Always Invalid**

**Check:**
1. Server time is accurate (UTC)
2. Database `ExpiryDate` is in UTC
3. OTP hasn't expired
4. Code entered correctly (no spaces)

### **Issue: Multiple OTPs Received**

**Expected Behavior:** Only the latest OTP is valid
- System automatically invalidates old OTPs when new one is requested
- This is a security feature, not a bug

---

**Status:** ? **2FA Email OTP Registration Fully Implemented**  
**Version:** v1.0  
**Last Updated:** January 2025  
**OTP Validity:** 10 minutes  
**Security Level:** High
