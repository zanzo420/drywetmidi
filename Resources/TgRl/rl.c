#include <CoreFoundation/CoreFoundation.h>
#include <pthread.h>
#include <mach/mach_time.h>
#include <mach/mach.h>

typedef struct
{
    pthread_t thread;
    char active;
	void (*callback)(void);
} TickGeneratorInfo;

typedef struct
{
    pthread_t thread;
    char active;
	CFRunLoopRef runLoopRef;
} SessionInfo;

void SetPriorityRealtime()
{
    mach_timebase_info_data_t timebase;
    kern_return_t kr = mach_timebase_info(&timebase);

    // Set the thread priority.
    struct thread_time_constraint_policy ttcpolicy;
    thread_port_t threadport = pthread_mach_thread_np(pthread_self());

    // In ticks. Therefore to convert nanoseconds to ticks multiply by (timebase.denom / timebase.numer).
    ttcpolicy.period = 500 * 1000 * timebase.denom / timebase.numer; // Period over which we demand scheduling.
    ttcpolicy.computation = 100 * 1000 * timebase.denom / timebase.numer; // Minimum time in a period where we must be running.
    ttcpolicy.constraint = 100 * 1000 * timebase.denom / timebase.numer; // Maximum time between start and end of our computation in the period.
    ttcpolicy.preemptible = FALSE;

    kr = thread_policy_set(threadport, THREAD_TIME_CONSTRAINT_POLICY, (thread_policy_t)&ttcpolicy, THREAD_TIME_CONSTRAINT_POLICY_COUNT);
}

void TimerCallBack(CFRunLoopTimerRef timer, void *info)
{
	TickGeneratorInfo* tickGeneratorInfo = (TickGeneratorInfo*)info;
	tickGeneratorInfo->callback();
}

void SessionCallback(CFRunLoopTimerRef timer, void *info)
{
}

void* SessionThreadRoutine(void* data)
{
    SessionInfo* sessionInfo = (SessionInfo*)data;

    CFRunLoopTimerContext context = { 0, NULL, NULL, NULL, NULL };
	CFRunLoopTimerRef timerRef = CFRunLoopTimerCreate(
	    NULL,
		CFAbsoluteTimeGetCurrent() + 60,
		60,
		0,
		0,
		SessionCallback,
		&context);

    CFRunLoopRef runLoopRef = CFRunLoopGetCurrent();
	CFRunLoopAddTimer(runLoopRef, timerRef, kCFRunLoopDefaultMode);
	
	SetPriorityRealtime();

    sessionInfo->active = 1;
	sessionInfo->runLoopRef = runLoopRef;

    CFRunLoopRun();

    return NULL;
}

SessionInfo* CreateSession()
{
	SessionInfo* sessionInfo = malloc(sizeof(SessionInfo));
	
	sessionInfo->active = 0;

    pthread_create(&sessionInfo->thread, NULL, SessionThreadRoutine, sessionInfo);

    while (sessionInfo->active == 0) {}
	
	return sessionInfo;
}

void StartTimer(SessionInfo* sessionInfo, int ms, void (*callback)(void))
{
	double interval = (double)ms / 1000.0;
	
	CFRunLoopTimerContext context = { 0, data, NULL, NULL, NULL };
	CFRunLoopTimerRef timerRef = CFRunLoopTimerCreate(
	    NULL,
		CFAbsoluteTimeGetCurrent() + interval,
		interval,
		0,
		0,
		TimerCallBack,
		&context);

	CFRunLoopAddTimer(sessionInfo->runLoopRef, timerRef, kCFRunLoopDefaultMode);
}