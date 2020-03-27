#include <Windows.h>
#include "pch.h"
#define EXPORT extern "C" __declspec(dllexport)

/* Get the screen resolution in pixels. */
EXPORT void GetScreenResolution (int& width, int& height)
{
	SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_SYSTEM_AWARE);
	SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_SYSTEM_AWARE);
	SetProcessDPIAware();
	width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
	height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
}

/* Gets the x-position, y-position, width, and height of the given window handle. */
EXPORT bool GetWindowCoords(HWND handle, int& x, int& y, int& width, int& height) {
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
EXPORT void StyleWindow(HWND handle) {
	SetWindowLongPtr(handle, GWL_EXSTYLE, WS_EX_APPWINDOW | WS_EX_TOPMOST);
	SetWindowLongPtr(handle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
	SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW | SW_MAXIMIZE);
}

/* Sets the given window's x-position, y-position, width and height. */
EXPORT void SetWindowCoords(HWND handle, int x, int y, int cx, int cy) {
	SetWindowPos(handle, HWND_TOPMOST, x, y, cx, cy, SWP_NOREDRAW);
}