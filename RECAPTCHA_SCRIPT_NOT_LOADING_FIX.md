# ?? reCAPTCHA Script Not Loading - CRITICAL FIX

## ? **Problem**

The reCAPTCHA script is **NOT appearing** in the Network tab:
```
? Missing: https://www.google.com/recaptcha/api.js?render=...
```

This means the `<script>` tag is **not being rendered** in the HTML.

---

## ?? **Root Cause**

The `@Model.ReCaptchaSiteKey` variable is likely **empty or null**, causing the script tag to render incorrectly:

```html
<!-- WRONG: This won't load -->
<script src="https://www.google.com/recaptcha/api.js?render="></script>

<!-- CORRECT: Should be -->
<script src="https://www.google.com/recaptcha/api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0"></script>
```

---

## ? **IMMEDIATE FIX: Diagnostic Test**

I've created a **diagnostic page** to identify the exact issue.

### **Step 1: Restart Your Application**

1. **Stop debugging** (Shift+F5)
2. **Rebuild** (Ctrl+Shift+B)
3. **Start debugging** (F5)

### **Step 2: Navigate to Diagnostic Page**

```
http://localhost:5033/RecaptchaTest
```

### **Step 3: Check the Results**

#### **Scenario A: Site Key is Loaded ?**

**Page shows:**
```
Site Key from Service: 6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0 ? OK
Expected Site Key: ? MATCH
Script Loading: ? SUCCESS
```

**Console shows:**
```
?? Site Key from Model: 6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0
? reCAPTCHA script loaded successfully
? grecaptcha object is available
```

**Network Tab shows:**
```
? api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0 ? 200 OK
```

**? If this works:**
- The configuration IS correct
- The problem is specific to the Register page
- **Solution:** Check if you restarted after modifying `Register.cshtml`

---

#### **Scenario B: Site Key is Empty ?**

**Page shows:**
```
Site Key from Service: (empty) ? EMPTY!
Expected Site Key: ? MISMATCH
Script Loading: ? CRITICAL ERROR: Site key is empty!
```

**Console shows:**
```
? CRITICAL: Site key is empty! Cannot load reCAPTCHA script.
?? Site Key from Model: 
?? Site Key Length: 0
```

**Network Tab shows:**
```
? No requests to google.com/recaptcha
```

**? If this happens:**
- Configuration is NOT being loaded
- **Solution:** Follow Fix #2 below

---

## ?? **Fix #2: Configuration Not Loading**

### **Verify appsettings.json**

Check that your `appsettings.json` has the correct structure:

```json
{
  "ConnectionStrings": {
 "DefaultConnection": "..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "GoogleReCaptcha": {
  "SiteKey": "6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0",
"SecretKey": "6Ld9yFMsAAAAACdqkL-boq-1oLaRfHkJFA7G3WMS",
    "Version": "v3",
    "MinimumScore": 0.5
  }
}
```

**Common Mistakes:**

? **Wrong indentation:**
```json
{
  "GoogleReCaptcha": {
  "SiteKey": "..."  // Missing proper indentation
  }
}
```

? **Typo in section name:**
```json
{
  "GoogleRecaptcha": { ... }  // Should be "GoogleReCaptcha" (capital C)
}
```

? **Missing comma:**
```json
{
  "AllowedHosts": "*"  // Missing comma here!
  "GoogleReCaptcha": { ... }
}
```

### **Verify Program.cs Registration**

Check `Program.cs` has this:

```csharp
// Should be near the top, before var app = builder.Build();
builder.Services.Configure<GoogleReCaptchaConfig>(
    builder.Configuration.GetSection("GoogleReCaptcha"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IReCaptchaService, ReCaptchaService>();
```

**? This should already be there** (I checked your code earlier).

---

## ?? **Fix #3: Hard-Code Site Key (Temporary Test)**

If the diagnostic page shows empty site key, temporarily hard-code it to test:

### **Update Register.cshtml.cs:**

```csharp
public void OnGet()
{
    // TEMPORARY: Hard-code for testing
    ReCaptchaSiteKey = "6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0";
    
    // Original code (commented out for testing):
    // ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
}
```

**Test again:**
1. Stop debugging (Shift+F5)
2. Start debugging (F5)
3. Go to `/Register`
4. Check Network tab

**If this works:**
- ? The problem is in the service/configuration
- ? The service is returning empty string

**If this still doesn't work:**
- ? Something else is wrong (Razor page not rendering, browser cache, etc.)

---

## ?? **Fix #4: Clear Browser Cache**

Sometimes the browser caches the broken page.

### **Hard Refresh:**

**Chrome/Edge:**
```
Ctrl + Shift + R  (Windows)
Cmd + Shift + R   (Mac)
```

**Or:**
1. Open DevTools (F12)
2. **Right-click** the refresh button
3. Select **Empty Cache and Hard Reload**

---

## ?? **Fix #5: Check for Razor Syntax Errors**

View the **rendered HTML source** to see what's actually being output:

1. Go to `/Register`
2. **Right-click** on page ? **View Page Source**
3. Search for "recaptcha"

**Look for:**

? **If you see this (BROKEN):**
```html
<script src="https://www.google.com/recaptcha/api.js?render="></script>
<!-- Empty render parameter! -->
```

? **Should be this (WORKING):**
```html
<script src="https://www.google.com/recaptcha/api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0"></script>
```

? **If you see this (RAZOR ERROR):**
```html
<script src="https://www.google.com/recaptcha/api.js?render=@Model.ReCaptchaSiteKey"></script>
<!-- Literal @Model... means Razor didn't process it! -->
```

**If Razor isn't processing:**
- The `@section Scripts` might not be in the correct place
- The layout page might not have `@RenderSection("Scripts", required: false)`

---

## ?? **Quick Verification Checklist**

Run through this checklist:

- [ ] ? **Restarted** Visual Studio debug session
- [ ] ? **Navigated** to `/RecaptchaTest` diagnostic page
- [ ] ? **Checked** diagnostic results (Site Key shown?)
- [ ] ? **Verified** `appsettings.json` has correct keys
- [ ] ? **Checked** `Program.cs` has service registration
- [ ] ? **Hard refreshed** browser (Ctrl+Shift+R)
- [ ] ? **Viewed** page source to see rendered HTML
- [ ] ? **Checked** Network tab for script request

---

## ?? **Expected vs Actual**

### **What You Should See:**

**On `/Register` page:**

**Network Tab:**
```
? api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0
   Status: 200 OK
   Type: script
   Initiator: Register (line X)
```

**Console:**
```
? Google reCAPTCHA v3 loaded successfully
?? reCAPTCHA Site Key: 6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0
? grecaptcha object is available
```

**Page Source:**
```html
<script src="https://www.google.com/recaptcha/api.js?render=6Ld9yFMsAAAAADcSGpPBM7usj0MSmr2Env7naSZ0" 
        onload="console.log('? Google reCAPTCHA v3 loaded successfully');"
    onerror="console.error('? Failed to load Google reCAPTCHA v3');"></script>
```

---

### **What You're Currently Seeing:**

**Network Tab:**
```
? No requests to google.com/recaptcha at all
```

**Console:**
```
(No reCAPTCHA related messages)
```

**This means:** The `<script>` tag is either:
1. Not being rendered (Site Key is empty)
2. Rendered incorrectly (Syntax error)
3. Blocked before it can execute (CSP, ad blocker)

---

## ?? **Action Plan**

### **Step 1: Run Diagnostic** ? **DO THIS FIRST**

```
1. Stop debugging (Shift+F5)
2. Start debugging (F5)
3. Navigate to: http://localhost:5033/RecaptchaTest
4. Check the results on the page
5. Check the Console
6. Check the Network tab
```

### **Step 2: Based on Diagnostic Results**

**If diagnostic shows site key:**
- ? Configuration is OK
- Problem is specific to Register page
- Solution: Restart application and hard refresh browser

**If diagnostic shows empty site key:**
- ? Configuration problem
- Solution: Fix `appsettings.json` (see Fix #2)
- Or: Hard-code site key temporarily (see Fix #3)

### **Step 3: Report Back**

Once you've run the diagnostic page, let me know:

1. **What does the diagnostic page show?**
   - Site key value?
 - Script loading status?
   
2. **What's in the Console?**
   - Copy all messages

3. **What's in the Network tab?**
   - Any requests to google.com?

4. **View page source**
   - What does the `<script>` tag look like?

---

## ?? **Files Created**

| File | Purpose |
|------|---------|
| `RecaptchaTest.cshtml` | Diagnostic page UI |
| `RecaptchaTest.cshtml.cs` | Diagnostic page logic |

---

**Status:** ? **ACTION REQUIRED**  
**Next Step:** Navigate to `/RecaptchaTest` and report results  
**Last Updated:** January 2025
