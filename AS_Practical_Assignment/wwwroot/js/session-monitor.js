// Auto Session Checker - Automatically logs out user when session is invalidated
// (e.g., when another login is detected)

(function() {
    'use strict';

  let sessionCheckInterval = null;
const CHECK_INTERVAL_MS = 5000; // Check every 5 seconds
    const INITIAL_DELAY_MS = 20000; // Wait 20 seconds after page load before first check (increased for reliability)

    /**
   * Check if the current session is still valid
     */
    async function checkSessionStatus() {
        try {
            const response = await fetch('/Api/CheckSession', {
     method: 'GET',
      headers: {
  'Accept': 'application/json'
},
         credentials: 'same-origin',
 cache: 'no-cache'
       });

         if (response.ok) {
   const data = await response.json();
    
      if (!data.isValid) {
 handleInvalidSession(data.reason);
          }
} else if (response.status === 401 || response.status === 403) {
     // Unauthorized - session already expired
    handleInvalidSession('unauthorized');
    }
        } catch (error) {
         console.error('Session check failed:', error);
  // Don't logout on network errors, just log it
     }
    }

    /**
     * Handle invalid session - show message and redirect to logout
     */
    function handleInvalidSession(reason) {
        console.log('Session invalidated:', reason);
      
    // Stop checking
   if (sessionCheckInterval) {
      clearInterval(sessionCheckInterval);
 sessionCheckInterval = null;
        }
     
    // Determine message based on reason
        let message = '?? Session Expired\n\nYour session has expired. Please log in again.';
 
    if (reason === 'session_invalidated') {
message = '?? Multiple Login Detected!\n\n' +
       'Your account has been accessed from another location.\n' +
  'You will be logged out for security.';
 }
        
  // Show alert
alert(message);
        
        // Force logout
     window.location.href = '/Logout';
    }

  /**
     * Start session monitoring
     */
    function startSessionMonitoring() {
    // Only run if user is authenticated
        const isAuthenticated = document.body.dataset.authenticated === 'true';
        
        if (!isAuthenticated) {
      return;
  }

      // Check if this is a fresh login (redirect from login page)
        // If so, increase the delay even more to allow session to stabilize
   const isFreshLogin = document.referrer && (
document.referrer.includes('/Login') || 
 document.referrer.includes('/Register')
    );
        
        const delay = isFreshLogin ? 30000 : INITIAL_DELAY_MS; // 30 seconds for fresh login, 20 seconds otherwise
        
        console.log('Session monitoring will start in', delay / 1000, 'seconds');
        console.log('Fresh login detected:', isFreshLogin);
        console.log('Then checking every', CHECK_INTERVAL_MS / 1000, 'seconds');
        
   // Wait before first check (to allow session to be fully established after login)
        setTimeout(() => {
    console.log('Session monitoring started');
  
// Initial check
          checkSessionStatus();
   
  // Set up interval
        sessionCheckInterval = setInterval(checkSessionStatus, CHECK_INTERVAL_MS);
        }, delay);
 
     // Cleanup on page unload
      window.addEventListener('beforeunload', stopSessionMonitoring);
    }

    /**
     * Stop session monitoring
     */
    function stopSessionMonitoring() {
     if (sessionCheckInterval) {
     clearInterval(sessionCheckInterval);
     sessionCheckInterval = null;
       console.log('Session monitoring stopped');
        }
 }

    // Start monitoring when DOM is ready
    if (document.readyState === 'loading') {
 document.addEventListener('DOMContentLoaded', startSessionMonitoring);
    } else {
  startSessionMonitoring();
    }

    // Expose functions globally if needed
    window.SessionMonitor = {
      start: startSessionMonitoring,
        stop: stopSessionMonitoring,
 checkNow: checkSessionStatus
 };
})();
