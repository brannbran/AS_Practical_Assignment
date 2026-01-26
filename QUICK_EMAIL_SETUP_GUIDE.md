# ? Quick Email Setup Guide

## ?? **Quick Start (5 Minutes)**

Follow these steps to configure email for OTP verification:

---

## **Option 1: Gmail (Recommended for Testing)**

### **Step 1: Enable 2-Step Verification**

1. Go to your Google Account: https://myaccount.google.com/security
2. Under "Signing in to Google", select **2-Step Verification**
3. Click **Get Started** and follow the prompts

### **Step 2: Generate App Password**

1. Go to: https://myaccount.google.com/apppasswords
2. **Select app:** Choose "Mail"
3. **Select device:** Choose "Other (Custom name)"
4. **Enter name:** Type "Ace Job Agency"
5. Click **Generate**
6. **Copy the 16-character password** (e.g., `abcd efgh ijkl mnop`)

### **Step 3: Update appsettings.json**

Open `AS_Practical_Assignment/appsettings.json` and update:

```json
{
"EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
"SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Ace Job Agency",
    "Username": "your-email@gmail.com",
    "Password": "abcd efgh ijkl mnop",
    "EnableSsl": true
  }
}
```

**Replace:**
- `your-email@gmail.com` with your actual Gmail address
- `abcd efgh ijkl mnop` with your generated app password (remove spaces: `abcdefghijklmnop`)

### **Step 4: Test**

1. Run your application (F5)
2. Go to `/Register`
3. Fill out the form
4. Click "Send Verification Code"
5. Check your Gmail inbox for the OTP email

---

## **Option 2: Outlook/Hotmail**

### **Step 1: Enable 2-Step Verification**

1. Go to: https://account.microsoft.com/security
2. Under **Advanced security options**, turn on **Two-step verification**

### **Step 2: Generate App Password**

1. On the same page, find **App passwords**
2. Click **Create a new app password**
3. Copy the generated password

### **Step 3: Update appsettings.json**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@outlook.com",
    "SenderName": "Ace Job Agency",
    "Username": "your-email@outlook.com",
  "Password": "YOUR_APP_PASSWORD",
    "EnableSsl": true
  }
}
```

---

## **Option 3: SendGrid (Production Recommended)**

SendGrid is free for up to 100 emails/day.

### **Step 1: Create SendGrid Account**

1. Go to: https://signup.sendgrid.com/
2. Sign up for a free account

### **Step 2: Create API Key**

1. Go to **Settings** ? **API Keys**
2. Click **Create API Key**
3. Name it "Ace Job Agency"
4. Choose **Full Access**
5. Copy the API key

### **Step 3: Verify Sender**

1. Go to **Settings** ? **Sender Authentication**
2. Click **Verify a Single Sender**
3. Fill in your email and details
4. Check your email for verification link

### **Step 4: Update appsettings.json**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
  "SenderEmail": "verified@yourdomain.com",
    "SenderName": "Ace Job Agency",
    "Username": "apikey",
    "Password": "YOUR_SENDGRID_API_KEY",
    "EnableSsl": true
  }
}
```

---

## ?? **Testing Your Configuration**

### **Quick Test**

1. **Start your application** (F5 in Visual Studio)
2. **Navigate to** `/Register`
3. **Fill out the form** with:
   - Your email address
 - Valid password (12+ chars, complexity)
   - Other required fields
4. **Click** "Send Verification Code"
5. **Check your email** - you should receive a 6-digit code within seconds

### **Expected Email**

**Subject:** "Your Verification Code - Ace Job Agency"

**Content:**
```
Hello [Your Name],

Thank you for registering with Ace Job Agency! To complete your 
registration, please verify your email address.

Your Verification Code:
???????????????
?   123456?  ? 6-digit code
???????????????
Valid for 10 minutes

[Security warnings and instructions]

Best regards,
Ace Job Agency Team
```

### **What If Email Doesn't Arrive?**

1. **Check spam/junk folder**
2. **Wait 1-2 minutes** (sometimes delayed)
3. **Check Visual Studio Output** for errors:
   ```
   View ? Output ? Show output from: Debug
   ```
4. **Look for audit logs** in database:
 ```sql
   SELECT TOP 10 * FROM AuditLogs 
 WHERE Action LIKE '%OTP%' OR Action LIKE '%Email%'
   ORDER BY Timestamp DESC
   ```

---

## ?? **Common Issues & Solutions**

### **Issue 1: "Failed to send email"**

**Error in logs:** `Failed to send email to ...`

**Solutions:**
1. **Check credentials** in `appsettings.json`
2. **Verify app password** is correct (no spaces)
3. **Check firewall** - allow outbound port 587
4. **Test SMTP connection:**
   ```powershell
   Test-NetConnection smtp.gmail.com -Port 587
   ```

---

### **Issue 2: "Authentication failed"**

**Error:** `5.7.8 Username and Password not accepted`

**Solutions:**
1. **For Gmail:**
   - Ensure 2-Step Verification is enabled
   - Use App Password, not regular password
   - Remove spaces from app password
2. **For Outlook:**
   - Enable 2FA
   - Generate new app password
3. **Check username** matches sender email

---

### **Issue 3: "Mailbox unavailable"**

**Error:** `5.1.1 Mailbox does not exist`

**Solutions:**
1. **Verify sender email** exists
2. **For SendGrid:** Verify sender identity
3. **Check typos** in email address

---

### **Issue 4: Emails go to spam**

**Solution:**
1. **Add sender to contacts**
2. **For production:** Set up SPF, DKIM, DMARC records
3. **Use professional email service** like SendGrid

---

## ?? **Configuration Examples**

### **Development (Testing)**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "testaccount@gmail.com",
    "SenderName": "Ace Job Agency [DEV]",
    "Username": "testaccount@gmail.com",
    "Password": "app-password-here",
    "EnableSsl": true
  }
}
```

### **Production**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SenderEmail": "noreply@acejobagency.com",
    "SenderName": "Ace Job Agency",
    "Username": "apikey",
    "Password": "SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
"EnableSsl": true
  }
}
```

---

## ?? **Security Notes**

### **DO:**
? Use app passwords, not regular passwords  
? Enable 2-Factor Authentication on email account  
? Use environment variables for production passwords  
? Keep `appsettings.json` out of source control  
? Use `.gitignore` to exclude sensitive files  

### **DON'T:**
? Commit passwords to Git repository  
? Use personal email for production  
? Share SMTP credentials  
? Use same password for multiple services  

---

## ?? **Next Steps**

After email is configured:

1. **Test registration flow:**
   - `/Register` ? Fill form ? Send code
   - Check email ? Get 6-digit code
   - `/VerifyEmail` ? Enter code ? Complete registration

2. **Verify audit logs:**
   ```sql
   SELECT * FROM AuditLogs WHERE Action LIKE '%OTP%'
   ```

3. **Check user created:**
   ```sql
   SELECT Email, EmailConfirmed, CreatedDate 
   FROM AspNetUsers 
   ORDER BY CreatedDate DESC
   ```

4. **Test welcome email** - Should arrive after verification

---

## ?? **Email Provider Comparison**

| Provider | Free Tier | Setup Time | Reliability | Production Ready |
|----------|-----------|------------|-------------|------------------|
| **Gmail** | ? Unlimited | 2 min | High | ?? Limited (100/day recommended) |
| **Outlook** | ? Unlimited | 3 min | High | ?? Limited (100/day recommended) |
| **SendGrid** | ? 100/day | 5 min | Very High | ? Yes |
| **Mailgun** | ? 5000/month | 5 min | Very High | ? Yes |
| **AWS SES** | ? 62,000/month | 10 min | Very High | ? Yes |

**Recommendation:**
- **Development:** Gmail (easiest)
- **Production:** SendGrid or AWS SES (most reliable)

---

## ? **Verification Checklist**

- [ ] Email credentials configured in `appsettings.json`
- [ ] Application builds successfully
- [ ] Registration form accessible at `/Register`
- [ ] reCAPTCHA v3 working (badge visible)
- [ ] Form submits without errors
- [ ] OTP email received in inbox
- [ ] `/VerifyEmail` page loads correctly
- [ ] OTP code validation works
- [ ] Account created after successful verification
- [ ] Welcome email received
- [ ] User automatically logged in
- [ ] Audit logs show all OTP activities

---

**Need Help?**  
Check the main documentation: `EMAIL_OTP_2FA_REGISTRATION_GUIDE.md`

**Status:** Ready for Testing  
**Estimated Setup Time:** 5-10 minutes  
**Difficulty:** Easy ??
