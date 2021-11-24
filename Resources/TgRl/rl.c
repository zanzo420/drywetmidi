#include <CoreFoundation/CoreFoundation.h>
#include <pthread.h>
#include <mach/mach_time.h>

typedef struct
{
    pthread_t thread;
    char active;
	void (*callback)(void);
} TickGeneratorInfo;

void SetPriorityRealtime()
{
    mach_timebase_info_data_t timebase;
    kern_return_t kr = mach_timebase_info(&timebase);

    // Set the thread priority.
    thread_time_constraint_policy ttcpolicy;
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

void* RunLoopThreadRoutine(void* data)
{
    TickGeneratorInfo* tickGeneratorInfo = (TickGeneratorInfo*)data;

    CFRunLoopTimerContext context = { 0, data, NULL, NULL, NULL };
	CFRunLoopTimerRef timerRef = CFRunLoopTimerCreate(
	    NULL,
		CFAbsoluteTimeGetCurrent() + 0.001,
		0.001,
		0,
		0,
		TimerCallBack,
		&context);

    CFRunLoopRef runLoopRef = CFRunLoopGetCurrent();
	CFRunLoopAddTimer(runLoopRef, timerRef, kCFRunLoopDefaultMode);
	
    tickGeneratorInfo->active = 1;
	SetPriorityRealtime();

    CFRunLoopRun();

    return NULL;
}

void CreateTimer(void (*callback)(void))
{
	TickGeneratorInfo* tickGeneratorInfo = malloc(sizeof(TickGeneratorInfo));

    tickGeneratorInfo->active = 0;
	tickGeneratorInfo->callback = callback;

    pthread_create(&tickGeneratorInfo->thread, NULL, RunLoopThreadRoutine, tickGeneratorInfo);

    while (tickGeneratorInfo->active == 0) {}
}