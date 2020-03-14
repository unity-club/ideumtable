#include <Windows.h>
#define EXPORT extern "C" __declspec(dllexport)

/* Get the screen resolution in pixels. */
EXPORT void getScreenResolution (int& width, int& height)
{
	/*
	 * Taken from StackOverflow: https://stackoverflow.com/questions/8690619/how-to-get-screen-resolution-in-c 
	 */
	RECT desktop;
	// Get a handle to the desktop window
	const HWND hDesktop = GetDesktopWindow();
	// Get the size of screen to the variable desktop
	GetWindowRect(hDesktop, &desktop);
	// The top left corner will have coordinates (0,0)
	// and the bottom right corner will have coordinates
	// (horizontal, vertical)
	width = desktop.right;
	height = desktop.bottom;
}

/* Gets the x-position, y-position, width, and height of the given window handle. */
EXPORT bool GetWindowRect(HWND handle, int& x, int& y, int& width, int& height) {
	RECT rect;
	if (GetWindowRect(handle, &rect)) {
		x = rect.left;
		y = rect.top;
		width = rect.right - x;
		height = rect.bottom - y;
		return true;
	}
	else return false;
}

/* Styles the given window accordingly so that it is borderless. */
EXPORT void setBordlerless(HWND handle, bool flag) {
	if (flag) {
		SetWindowLongPtr(handle, GWL_STYLE, WS_POPUPWINDOW | WS_VISIBLE);
	}
	else {
		SetWindowLongPtr(handle, GWL_STYLE, WS_SYSMENU | WS_VISIBLE);
	}
	SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
}

/* Sets the given window's x and y positions. */
EXPORT void setWindowPos(HWND handle, int x, int y) {
	SetWindowPos(handle, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW);
}

/* Sets the given window's width and height. */
EXPORT void setWindowSize(HWND handle, int cx, int cy) {
	SetWindowPos(handle, HWND_TOPMOST, 0, 0, cx, cy, SWP_NOMOVE | SWP_SHOWWINDOW);
}

/* Sets the given window's x-position, y-position, width and height. */
EXPORT void setWindowRect(HWND handle, int x, int y, int cx, int cy) {
	SetWindowPos(handle, HWND_TOPMOST, x, y, cx, cy, SWP_SHOWWINDOW);
}