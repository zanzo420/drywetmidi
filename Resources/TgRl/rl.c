#include <CoreFoundation/CoreFoundation.h>
#include <pthread.h>
#include <mach/mach_time.h>

typedef struct
{
    pthread_t thread;
    char active;
	void (*callback)(void);
} TickGeneratorInfo;

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