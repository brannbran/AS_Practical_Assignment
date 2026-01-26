# ?? CodeQL Advanced Setup Conflict - Resolution Guide

## ?? **Error**

GitHub Actions failed with:

```
Code Scanning could not process the submitted SARIF file:
CodeQL analyses from advanced configurations cannot be processed when the default setup is enabled
```

**Both languages (JavaScript & C#) failed with this error.**

---

## ?? **Root Cause**

GitHub has **two modes** for CodeQL:

| Mode | Description | Configuration |
|------|-------------|---------------|
| **Default Setup** | ? Enabled on GitHub | No config files needed |
| **Advanced Setup** | ?? Custom workflow | Uses `.github/workflows/codeql.yml` |

**You cannot use both at the same time!**

Your repository has:
- ? **Default setup enabled** (on GitHub)
- ? **Advanced config present** (in repository)

This causes a conflict.

---

## ? **Solution: Disable Default Setup**

Since you have a custom configuration that excludes third-party libraries (recommended), you should **disable the default setup**.

### **Step-by-Step Instructions**

#### **1. Go to Repository Settings**

Navigate to:
```
https://github.com/brannbran/AS_Practical_Assignment/settings/security_analysis
```

Or manually:
1. Go to your repository
2. Click **Settings** (top right)
3. Click **Code security and analysis** (left sidebar)

---

#### **2. Disable Default CodeQL Setup**

Scroll to the **Code scanning** section:

1. Find **CodeQL analysis**
2. You'll see: `Default setup: Enabled`
3. Click the **?** (three dots) or **Configure** button
4. Select **Disable CodeQL**
5. Confirm the action

**Screenshot:**
```
?????????????????????????????????????????????????
?  Code scanning       ?
?       ?
?  CodeQL analysis    ?
?  Status: Default setup (Enabled)  ?
?  [Configure ?] [Disable]       ?
?????????????????????????????????????????????????
```

---

#### **3. Verify Workflow File**

Make sure your `.github/workflows/codeql.yml` exists and is valid.

**Current Status:** ? **Fixed** (indentation corrected in commit `c48c00b`)

**File:** `.github/workflows/codeql.yml`

```yaml
name: "CodeQL Analysis"

on:
  push:
    branches: [ "master", "main" ]
  pull_request:
    branches: [ "master", "main" ]
  schedule:
    - cron: '0 0 * * 1'

jobs:
  analyze:
    name: Analyze
    runs-on: ubuntu-latest
    permissions:
      actions: read
      contents: read
      security-events: write

    strategy:
      fail-fast: false
      matrix:
        language: [ 'javascript', 'csharp' ]

 steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
       languages: ${{ matrix.language }}
          config-file: ./.github/codeql/codeql-config.yml
   queries: security-and-quality

      - name: Autobuild
        uses: github/codeql-action/autobuild@v3

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
        with:
      category: "/language:${{matrix.language}}"
```

---

#### **4. Wait for Next Workflow Run**

After disabling default setup:

1. **Automatic:** Wait for next push/PR (triggers workflow)
2. **Manual:** Go to **Actions** tab ? **CodeQL Analysis** ? **Run workflow**

---

## ?? **Why Use Advanced Configuration?**

Your advanced configuration has these benefits:

### **? Excludes Third-Party Libraries**

**File:** `.github/codeql/codeql-config.yml`

```yaml
paths-ignore:
  - 'AS_Practical_Assignment/wwwroot/lib/**'
  - '**/jquery*.js'
  - '**/bootstrap*.js'
  - '**/jquery.validate*.js'
  - '**/*.min.js'
```

**Benefits:**
- ? No parse errors from jQuery/Bootstrap
- ? Faster scans
- ? Focus on your code only
- ? Reduced false positives

### **? Custom Query Filters**

**File:** `.github/codeql-queries.yml`

Excludes queries that false-positive on libraries.

### **? Precise Path Control**

Only scans:
- `AS_Practical_Assignment/Pages/**`
- `AS_Practical_Assignment/Services/**`
- `AS_Practical_Assignment/Models/**`
- `AS_Practical_Assignment/wwwroot/js/**` (your custom JS)

---

## ?? **Alternative: Use Default Setup**

If you prefer simpler setup (not recommended):

### **Option: Remove Advanced Configuration**

```powershell
# Remove custom CodeQL files
git rm .github/workflows/codeql.yml
git rm .github/codeql/codeql-config.yml
git rm .github/codeql-queries.yml
git commit -m "chore: Remove advanced CodeQL config"
git push origin master
```

Then on GitHub:
1. **Settings** ? **Code security and analysis**
2. **Code scanning** ? **Set up** ? **Default**
3. Click **Enable CodeQL**

**?? Drawbacks:**
- ? Will scan third-party libraries
- ? May get parse errors
- ? Slower scans
- ? More false positives
- ? Less control

---

## ?? **Troubleshooting**

### **Issue: Workflow Still Fails After Disabling Default**

**Solution:** Wait 5-10 minutes for GitHub to process the change.

Or manually trigger:
```
Actions ? CodeQL Analysis ? Run workflow ? Run workflow
```

---

### **Issue: Don't See "Disable" Button**

**Possible causes:**
1. You're not a repository admin
2. Default setup is already disabled
3. Advanced setup is already active

**Check:**
```
Settings ? Code security and analysis ? Code scanning
```

Should show:
- **Advanced setup** with your workflow name

---

### **Issue: YAML Syntax Errors**

**Check indentation:**
```powershell
# Validate YAML syntax
cat .github/workflows/codeql.yml
```

**Common mistakes:**
- ? Tabs instead of spaces
- ? Inconsistent indentation
- ? Missing colons
- ? Wrong nesting level

**Fix applied in commit:** `c48c00b`

---

## ?? **Verify the Fix**

### **Step 1: Check Workflow Runs**

1. Go to **Actions** tab
2. Look for **CodeQL Analysis** workflow
3. Check latest run

**Expected:**
```
? Analyze (csharp)
? Analyze (javascript)
```

---

### **Step 2: Check Security Tab**

```
Security ? Code scanning ? View alerts
```

Should show:
- ? No configuration errors
- ? Alerts only from your custom code
- ? No alerts from third-party libraries

---

## ?? **Summary**

| Component | Status |
|-----------|--------|
| Workflow YAML | ? Fixed (commit `c48c00b`) |
| CodeQL Config | ? Exists |
| Query Filters | ? Exists |
| .gitattributes | ? Configured |
| Default Setup | ?? **ACTION REQUIRED: Disable on GitHub** |

---

## ?? **Next Steps**

### **1. Disable Default Setup** ? **CRITICAL**

```
GitHub ? Settings ? Code security ? Code scanning ? Disable CodeQL
```

### **2. Verify Workflow**

```
Actions ? CodeQL Analysis ? Check latest run
```

### **3. Monitor Results**

```
Security ? Code scanning ? View alerts
```

---

## ?? **Quick Reference**

### **Disable Default Setup**

```
https://github.com/brannbran/AS_Practical_Assignment/settings/security_analysis
?
Code scanning section
?
CodeQL analysis ? Disable
```

### **Manual Workflow Trigger**

```
https://github.com/brannbran/AS_Practical_Assignment/actions/workflows/codeql.yml
?
Run workflow ? Run workflow
```

### **Check Status**

```
https://github.com/brannbran/AS_Practical_Assignment/actions
?
CodeQL Analysis ? Latest run
```

---

## ? **Expected Final State**

**After completing all steps:**

```
GitHub Settings:
  ? Default CodeQL setup: DISABLED
  ? Advanced setup: ACTIVE (.github/workflows/codeql.yml)

GitHub Actions:
  ? CodeQL Analysis workflow: PASSING
  ? JavaScript analysis: SUCCESS
  ? C# analysis: SUCCESS

Security Tab:
  ? Code scanning alerts: Only from your code
  ? No parse errors
  ? No third-party library alerts
```

---

## ?? **Result**

**Before:**
```
? CodeQL analyses from advanced configurations cannot be processed
? Both JavaScript & C# scans failed
```

**After:**
```
? Advanced configuration active
? Default setup disabled
? Scans complete successfully
? Only your code is analyzed
```

---

**Status:** ?? **ACTION REQUIRED**  
**Last Updated:** January 2025  
**Commit:** `c48c00b`  

## ?? **IMPORTANT: You MUST disable default setup on GitHub for this to work!**
