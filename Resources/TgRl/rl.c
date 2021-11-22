#include <CoreFoundation/CoreFoundation.h>
#include <pthread.h>
#include <mach/mach_time.h>

typedef struct
{
    pthread_t thread;
    char active;
    int interval;
	CFRunLoopTimerCallBack callback;
} TickGeneratorInfo;

void* RunLoopThreadRoutine(void* data)
{
    TickGeneratorInfo* tickGeneratorInfo = (TickGeneratorInfo*)data;

    CFRunLoopTimerContext context = { 0, NULL, NULL, NULL, NULL };
	CFRunLoopTimerRef timerRef = CFRunLoopTimerCreate(
	    NULL,
		CFAbsoluteTimeGetCurrent() + 0.001,
		0.001,
		0,
		0,
		tickGeneratorInfo->callback,
		&context);

    CFRunLoopRef runLoopRef = CFRunLoopGetCurrent();
	CFRunLoopAddTimer(runLoopRef, timerRef, kCFRunLoopDefaultMode);
	
    tickGeneratorInfo->active = 1;

    CFRunLoopRun();

    return NULL;
}

void CreateTimer(int ms, CFRunLoopTimerCallBack callback)
{
	TickGeneratorInfo* tickGeneratorInfo = malloc(sizeof(TickGeneratorInfo));

    tickGeneratorInfo->active = 0;
    tickGeneratorInfo->interval = ms;
	tickGeneratorInfo->callback = callback;

    *info = tickGeneratorInfo;

    pthread_create(&tickGeneratorInfo->thread, NULL, RunLoopThreadRoutine, tickGeneratorInfo);

    while (tickGeneratorInfo->active == 0) {}
}