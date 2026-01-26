# ?? Google reCAPTCHA v3 Testing Guide

## ? **Your reCAPTCHA v3 Setup**

Based on your code, you have a complete reCAPTCHA v3 implementation on the **Register** page:

| Component | Status | Location |
|-----------|--------|----------|
| **Service** | ? Configured | `Services/ReCaptchaService.cs` |
| **Frontend Script** | ? Loaded | `Register.cshtml` |
| **Backend Validation** | ? Implemented | `Register.cshtml.cs` |
| **Configuration** | ? Set | `appsettings.json` |
| **Audit Logging** | ? Enabled | Logs passed/failed attempts |

**Configuration:**
- **Site Key:** `6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0`
- **Secret Key:** `6Ld9yFMsAAAAACdqkL-boq-1oLaRfHkJFA7G3WMS`
- **Minimum Score:** `0.5` (Medium threshold)
- **Action:** `register`

---

## ?? **How to Test reCAPTCHA v3**

### **Method 1: Browser Developer Tools (Easiest)**

#### **Step 1: Open Registration Page**
```
http://localhost:5033/Register
```

#### **Step 2: Open DevTools Console**
- Press **F12** (Windows) or **Cmd+Option+I** (Mac)
- Click **Console** tab

#### **Step 3: Check reCAPTCHA Loaded**

Look for this in the console:
```
? Google reCAPTCHA v3 loaded successfully
```

You should also see the reCAPTCHA badge in the bottom-right corner of the page:

```
???????????????????????????
? reCAPTCHA         ?
? protected by Google   ?
???????????????????????????
```

#### **Step 4: Fill Out the Form**

1. Enter all required fields
2. Before clicking "Register", check the **Network** tab in DevTools
3. Click **Register**

#### **Step 5: Monitor Network Requests**

In the **Network** tab, you should see:

**1. Request to Google reCAPTCHA API:**
```
https://www.google.com/recaptcha/api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0
Status: 200 OK
```

**2. Token Generation:**

In the Console, you can manually test:
```javascript
grecaptcha.ready(function() {
    grecaptcha.execute('6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0', {action: 'register'})
        .then(function(token) {
  console.log('? reCAPTCHA Token:', token);
            console.log('Token length:', token.length, 'characters');
        })
        .catch(function(error) {
            console.error('? reCAPTCHA Error:', error);
        });
});
```

**Expected Output:**
```
? reCAPTCHA Token: 03AGdBq24PxYZ... (very long string)
Token length: 392 characters
```

#### **Step 6: Check Form Submission**

In the **Network** tab, find the POST request to `/Register`:

**Request Payload should include:**
```
g-recaptcha-response: 03AGdBq24PxYZ... (long token)
```

---

### **Method 2: Check Backend Validation Logs**

#### **Step 1: Enable Detailed Logging**

Add this to `appsettings.json` **temporarily**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "AS_Practical_Assignment.Services.ReCaptchaService": "Information"
    }
  }
}
```

#### **Step 2: Run Application & Monitor Output**

In Visual Studio's **Output** window, you'll see:

**Successful Registration:**
```
info: AS_Practical_Assignment.Services.ReCaptchaService[0]
    reCAPTCHA validation successful. Score: 0.9, Action: register

info: AS_Practical_Assignment.Services.AuditService[0]
      Audit logged: reCAPTCHA Passed for user@example.com
```

**Failed reCAPTCHA:**
```
warn: AS_Practical_Assignment.Services.ReCaptchaService[0]
  reCAPTCHA score too low: 0.3 for action: register

warn: AS_Practical_Assignment.Pages.RegisterModel[0]
      reCAPTCHA validation failed for registration: user@example.com. Score: 0.3

info: AS_Practical_Assignment.Services.AuditService[0]
 Audit logged: reCAPTCHA Failed for user@example.com
```

---

### **Method 3: Check Database Audit Logs**

#### **Step 1: Register a Test User**

Go to `/Register` and create an account.

#### **Step 2: Check ActivityLog Page**

Navigate to:
```
http://localhost:5033/ActivityLog
```

Or check the database directly:
```sql
SELECT TOP 10 
 Action,
    Status,
    Description,
Details,
 Timestamp
FROM AuditLogs
WHERE Action IN ('reCAPTCHA Passed', 'reCAPTCHA Failed')
ORDER BY Timestamp DESC
```

**Expected Output:**

| Action | Status | Description | Details | Timestamp |
|--------|--------|-------------|---------|-----------|
| reCAPTCHA Passed | Success | reCAPTCHA validation successful for action: register | Score: 0.9 | 2025-01-26 |
| Register | Success | New user registered successfully | | 2025-01-26 |

---

### **Method 4: Test with Different Scenarios**

#### **Test 1: Normal User (Should Pass)**

1. Go to `/Register`
2. Fill form normally
3. Click "Register"

**Expected Result:**
- ? Score: 0.7 - 1.0
- ? Registration successful
- ? Audit log: "reCAPTCHA Passed"

---

#### **Test 2: Rapid Form Submission (May Fail)**

1. Go to `/Register`
2. Fill form very quickly
3. Click "Register" immediately

**Expected Result:**
- ?? Score: 0.3 - 0.6
- ?? May fail if score < 0.5
- ?? Error: "Bot detection failed"

---

#### **Test 3: Missing reCAPTCHA Token (Should Fail)**

**Manually edit the form in DevTools:**
```javascript
// In Console:
document.getElementById('registerForm').addEventListener('submit', function(e) {
    e.preventDefault();
    // Submit without waiting for reCAPTCHA
    this.submit();
}, true);
```

**Expected Result:**
- ? Error: "reCAPTCHA token is missing"
- ? Audit log: "reCAPTCHA Failed"

---

#### **Test 4: Invalid Token (Should Fail)**

**Manually edit the token in DevTools:**
```javascript
// Before submitting, change token value:
document.querySelector('input[name="g-recaptcha-response"]').value = 'invalid-token';
```

**Expected Result:**
- ? Error: "Bot detection failed"
- ? Validation message from Google
- ? Audit log: "reCAPTCHA Failed"

---

## ?? **Understanding reCAPTCHA v3 Scores**

| Score Range | Interpretation | Action |
|-------------|----------------|--------|
| **0.9 - 1.0** | ? Definitely Human | Allow |
| **0.7 - 0.8** | ? Likely Human | Allow |
| **0.5 - 0.6** | ?? Suspicious | Allow (your threshold) |
| **0.3 - 0.4** | ?? Likely Bot | Block |
| **0.0 - 0.2** | ? Definitely Bot | Block |

**Your Current Threshold:** `0.5` (Medium security)

---

## ?? **Advanced Testing Tools**

### **Method 5: Use Google's Admin Console**

1. Go to: [https://www.google.com/recaptcha/admin](https://www.google.com/recaptcha/admin)
2. Log in with your Google account
3. Find your site key: `6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0`
4. Click "Analytics"

**You'll see:**
- ? Total requests
- ? Score distribution
- ? Pass/Fail ratio
- ? Suspicious activity

---

### **Method 6: Programmatic Test**

Create a test endpoint:

```csharp
// Add to Pages/TestRecaptcha.cshtml.cs
public class TestRecaptchaModel : PageModel
{
    private readonly IReCaptchaService _reCaptchaService;

    public TestRecaptchaModel(IReCaptchaService reCaptchaService)
    {
        _reCaptchaService = reCaptchaService;
    }

    public string? Result { get; set; }
    public string SiteKey { get; set; } = string.Empty;

    public void OnGet()
    {
     SiteKey = _reCaptchaService.GetSiteKey();
    }

    public async Task<IActionResult> OnPostAsync(string token)
  {
        var result = await _reCaptchaService.ValidateAsync(
            token, 
       "test", 
  HttpContext.Connection.RemoteIpAddress?.ToString()
  );

        Result = $@"
? Valid: {result.IsValid}
?? Score: {result.Score}
?? Action: {result.Action}
?? Message: {result.Message}
? Errors: {string.Join(", ", result.Errors)}
";

      return Page();
    }
}
```

---

## ?? **Common Issues & Solutions**

### **Issue 1: Badge Not Showing**

**Problem:** reCAPTCHA badge missing in bottom-right corner

**Solution:**
```html
<!-- Check if script is loaded -->
<script src="https://www.google.com/recaptcha/api.js?render=YOUR_SITE_KEY"></script>
```

**Verify:**
```javascript
// In Console
console.log(typeof grecaptcha);
// Should output: "object"
```

---

### **Issue 2: "Invalid Site Key" Error**

**Problem:** Console shows: `ERROR for site owner: Invalid site key`

**Solutions:**
1. **Check domain whitelist** in Google reCAPTCHA Admin Console
2. **Add localhost** to allowed domains:
 - `localhost`
   - `127.0.0.1`
3. **Verify site key** matches `appsettings.json`

---

### **Issue 3: Score Always 0.0**

**Problem:** All requests get score of 0.0

**Causes:**
- ? Using test keys in production
- ? Secret key mismatch
- ? Domain not whitelisted
- ? Action mismatch

**Solution:**
```json
// appsettings.json - Verify these match Google Console
{
  "GoogleReCaptcha": {
    "SiteKey": "YOUR_ACTUAL_SITE_KEY",
    "SecretKey": "YOUR_ACTUAL_SECRET_KEY",
    "Version": "v3"
  }
}
```

---

### **Issue 4: "timeout-or-duplicate" Error**

**Problem:** Google returns error: `timeout-or-duplicate`

**Cause:** Token used twice or expired (tokens valid for 2 minutes)

**Solution:**
```javascript
// Always generate fresh token on submit
grecaptcha.ready(function() {
    grecaptcha.execute('SITE_KEY', {action: 'register'})
  .then(function(token) {
          // Use token immediately
            submitForm(token);
  });
});
```

---

## ? **Verification Checklist**

### **Frontend Checklist:**

- [ ] ? reCAPTCHA badge visible in bottom-right
- [ ] ? Console shows no errors
- [ ] ? `grecaptcha` object is defined
- [ ] ? Token is generated (check Network tab)
- [ ] ? Token is added to form data

### **Backend Checklist:**

- [ ] ? `ReCaptchaService` configured in `Program.cs`
- [ ] ? `appsettings.json` has correct keys
- [ ] ? Validation logs appear in Output window
- [ ] ? Audit logs created in database
- [ ] ? Error messages displayed to user

### **Google Console Checklist:**

- [ ] ? Site created in reCAPTCHA Admin
- [ ] ? `localhost` added to domains
- [ ] ? Keys match `appsettings.json`
- [ ] ? Analytics show requests

---

## ?? **Quick Test Script**

Run this in your browser console on `/Register`:

```javascript
// Complete reCAPTCHA v3 Test
(async function testReCaptcha() {
    console.log('?? Testing reCAPTCHA v3...\n');
    
    // Check if loaded
    if (typeof grecaptcha === 'undefined') {
        console.error('? reCAPTCHA not loaded!');
   return;
    }
    console.log('? reCAPTCHA loaded');
    
    // Check badge
    const badge = document.querySelector('.grecaptcha-badge');
    console.log(badge ? '? Badge visible' : '?? Badge not found');
  
    // Test token generation
    try {
   const token = await new Promise((resolve, reject) => {
   grecaptcha.ready(function() {
    grecaptcha.execute('6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0', {action: 'register'})
            .then(resolve)
      .catch(reject);
            });
        });
        
        console.log('? Token generated successfully');
        console.log(`?? Token length: ${token.length} characters`);
        console.log(`?? Token preview: ${token.substring(0, 50)}...`);
        console.log('\n?? reCAPTCHA v3 is working correctly!');
   
    } catch (error) {
        console.error('? Token generation failed:', error);
    }
})();
```

**Expected Output:**
```
?? Testing reCAPTCHA v3...

? reCAPTCHA loaded
? Badge visible
? Token generated successfully
?? Token length: 392 characters
?? Token preview: 03AGdBq24PxYZ8vQ7mHqFzX3jN2kLpR9sT1wU4vW...

?? reCAPTCHA v3 is working correctly!
```

---

## ?? **Final Verification**

### **Complete Registration Test:**

1. **Navigate to:** `http://localhost:5033/Register`
2. **Open DevTools** (F12)
3. **Run test script** (above) in Console
4. **Fill form** with test data
5. **Click "Register"**
6. **Check Output window** for logs
7. **Verify database** audit logs
8. **Check Google Console** analytics

**Success Indicators:**
- ? User registered successfully
- ? No error messages
- ? Audit log shows "reCAPTCHA Passed"
- ? Score > 0.5 in logs
- ? Google Analytics incremented

---

## ?? **Security Notes**

**Current Configuration:**
```json
{
  "MinimumScore": 0.5  // Medium security
}
```

**Recommendations:**

| Threshold | Use Case |
|-----------|----------|
| **0.3** | Low security, very lenient |
| **0.5** | ? **Your Current** - Medium security |
| **0.7** | High security, stricter |
| **0.9** | Very high security, may block real users |

**Best Practices:**
- ? Your implementation follows best practices
- ? Tokens validated server-side
- ? Scores logged for monitoring
- ? Audit trail maintained
- ? Error handling implemented

---

## ?? **Monitoring Dashboard**

Check these regularly:

1. **Application Logs:**
   ```
   Visual Studio ? Output ? Show output from: Debug
   ```

2. **Database Audit Logs:**
   ```
   /ActivityLog page
   ```

3. **Google reCAPTCHA Admin:**
   ```
   https://www.google.com/recaptcha/admin
   ```

---

**Status:** ? **reCAPTCHA v3 Fully Configured**  
**Version:** v3  
**Action:** `register`  
**Threshold:** 0.5  
**Last Updated:** January 2025
