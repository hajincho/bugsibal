"""Interactive 3D pendulum experiment. Install with: python -m pip install vpython"""

from math import asin, cos, degrees, pi, radians, sin, sqrt

from vpython import (
    box,
    button,
    canvas,
    checkbox,
    color,
    curve,
    cylinder,
    gdots,
    graph,
    gcurve,
    rate,
    slider,
    sphere,
    vector,
    wtext,
)


DT = 0.001
SWEEP_ANGLES = (5, 15, 30, 45, 60, 75)


def exact_period(angle_deg, length, gravity):
    """Finite-amplitude period using K(k) = pi / (2 AGM(1, sqrt(1-k^2)))."""
    k = sin(radians(angle_deg) / 2)
    arithmetic, geometric = 1.0, sqrt(1.0 - k * k)
    while abs(arithmetic - geometric) > 1e-14:
        arithmetic, geometric = (arithmetic + geometric) / 2, sqrt(arithmetic * geometric)
    return 2 * pi * sqrt(length / gravity) / arithmetic


def numerical_period(angle_deg, length, gravity, dt=0.0005, damping=0.0):
    """Measure a cycle from consecutive downward center crossings."""
    theta = radians(angle_deg)
    omega = 0.0
    elapsed = 0.0
    previous_theta = theta
    crossings = []
    for _ in range(int(30 / dt)):
        previous_theta = theta
        omega += (-gravity / length * sin(theta) - damping * omega) * dt
        theta += omega * dt
        elapsed += dt
        if previous_theta > 0 >= theta and omega < 0:
            fraction = previous_theta / (previous_theta - theta)
            crossings.append(elapsed - dt + fraction * dt)
            if len(crossings) == 2:
                return crossings[1] - crossings[0]
    return float("nan")


scene = canvas(
    title="3D 단진자 실험실 | Nonlinear Pendulum Lab",
    width=1000,
    height=620,
    background=vector(0.055, 0.075, 0.095),
    center=vector(0, -1.25, 0),
    forward=vector(-1.0, -0.35, -1.2),
)
scene.range = 3.6
scene.autoscale = False
scene.caption = "\n"

length = 2.0
gravity = 9.81
damping = 0.0
theta0 = radians(30)
theta = theta0
omega = 0.0
azimuth = radians(35)
speed = 1.0
running = False
sim_time = 0.0
last_crossing = None
measured_period = None
crossing_count = 0
small_angle_period = 2 * pi * sqrt(length / gravity)

ceiling = box(pos=vector(0, 0.14, 0), size=vector(1.0, 0.12, 0.7), color=vector(0.35, 0.42, 0.48))
support = cylinder(pos=vector(0, -0.03, 0), axis=vector(0, -0.18, 0), radius=0.045, color=color.white)
bob = sphere(pos=vector(0, -length, 0), radius=0.16, color=vector(0.97, 0.43, 0.19), make_trail=True, retain=1400)
rod = cylinder(pos=vector(0, 0, 0), axis=bob.pos, radius=0.025, color=vector(0.75, 0.82, 0.88))
path = curve(color=vector(0.25, 0.8, 0.72), radius=0.012)
ground = box(pos=vector(0, -3.25, 0), size=vector(6, 0.04, 6), color=vector(0.16, 0.21, 0.24), opacity=0.7)

plot = graph(
    title="각도-시간 기록 | Angle vs time",
    xtitle="시간 (s)",
    ytitle="각도 (deg)",
    width=1000,
    height=260,
    background=vector(0.08, 0.10, 0.12),
    foreground=color.white,
)
angle_trace = gcurve(graph=plot, color=vector(0.25, 0.8, 0.72), width=2)

status = wtext(text="준비됨 | Ready\n")
measurement = wtext(text="")
table_output = wtext(text="")


def bob_position(angle):
    direction = vector(sin(azimuth), 0, cos(azimuth))
    return vector(0, 0, 0) + length * sin(angle) * direction + vector(0, -length * cos(angle), 0)


def refresh_scene():
    global small_angle_period
    bob.pos = bob_position(theta)
    rod.axis = bob.pos - rod.pos * 0  # Keep the rod rooted at the pivot.
    rod.axis = bob.pos
    small_angle_period = 2 * pi * sqrt(length / gravity)
    update_readout()


def update_readout():
    reference = exact_period(degrees(abs(theta0)), length, gravity)
    measured = f"{measured_period:.4f} s" if measured_period is not None else "측정 대기"
    measurement.text = (
        f"\n초기각 {degrees(abs(theta0)):.1f}°   현재각 {degrees(theta):.1f}°   "
        f"주기 측정 {measured}   작은각 T₀ {small_angle_period:.4f} s   "
        f"유한각 이론 T {reference:.4f} s\n"
    )


def reset(_event=None):
    global theta, omega, sim_time, last_crossing, measured_period, crossing_count, running
    running = False
    theta, omega, sim_time = theta0, 0.0, 0.0
    last_crossing = None
    measured_period = None
    crossing_count = 0
    bob.clear_trail()
    path.clear()
    angle_trace.delete()
    status.text = "\n초기 조건으로 재설정됨 | Reset to initial conditions\n"
    refresh_scene()


def toggle_run(_event=None):
    global running
    running = not running
    status.text = "\n실험 중 | Running\n" if running else "\n일시 정지 | Paused\n"


def set_angle(control):
    global theta0, theta, omega
    theta0 = radians(control.value)
    if not running:
        theta, omega = theta0, 0.0
        bob.clear_trail()
        path.clear()
    refresh_scene()


def set_length(control):
    global length
    length = control.value
    support.axis = vector(0, -0.18, 0)
    refresh_scene()


def set_gravity(control):
    global gravity
    gravity = control.value
    refresh_scene()


def set_damping(control):
    global damping
    damping = control.value
    damping_value.text = f"{damping:.3f}"


def set_azimuth(control):
    global azimuth
    azimuth = radians(control.value)
    refresh_scene()


def set_speed(control):
    global speed
    speed = control.value
    speed_value.text = f"{speed:.1f}x"


def run_angle_sweep(_event=None):
    rows = []
    for angle in SWEEP_ANGLES:
        measured = numerical_period(angle, length, gravity)
        theory = exact_period(angle, length, gravity)
        error = 100 * (measured - small_angle_period) / small_angle_period
        rows.append(
            f"<tr><td>{angle}°</td><td>{measured:.4f} s</td>"
            f"<td>{small_angle_period:.4f} s</td><td>{theory:.4f} s</td>"
            f"<td>{error:+.2f}%</td></tr>"
        )
    table_output.text = (
        "<br><b>각도별 주기 비교 (감쇠 없음)</b><br>"
        "<table border='1' cellpadding='5'><tr><th>초기각</th><th>수치 측정</th>"
        "<th>작은각 공식</th><th>유한각 이론</th><th>공식 오차</th></tr>"
        + "".join(rows)
        + "</table><br>"
    )


scene.append_to_caption("초기 변위각  ")
angle_value = wtext(text="30°")
angle_slider = slider(min=5, max=85, value=30, length=240, bind=set_angle)
scene.append_to_caption("\n길이  ")
length_value = wtext(text="2.00 m")


def show_length(control):
    length_value.text = f"{control.value:.2f} m"
    set_length(control)


length_slider = slider(min=0.5, max=4, value=2, length=240, bind=show_length)
scene.append_to_caption("\n중력가속도  ")
gravity_value = wtext(text="9.81 m/s²")


def show_gravity(control):
    gravity_value.text = f"{control.value:.2f} m/s²"
    set_gravity(control)


gravity_slider = slider(min=1, max=20, value=9.81, length=240, bind=show_gravity)
scene.append_to_caption("\n진자 운동 평면 방위각  ")
azimuth_slider = slider(min=0, max=360, value=35, length=240, bind=set_azimuth)
scene.append_to_caption("\n감쇠 계수  ")
damping_slider = slider(min=0, max=0.12, value=0, length=240, bind=set_damping)
damping_value = wtext(text="0.000")
scene.append_to_caption("\n시뮬레이션 속도  ")
speed_slider = slider(min=0.25, max=4, value=1, length=240, bind=set_speed)
speed_value = wtext(text="1.0x")
scene.append_to_caption("\n\n")
button(text="시작 / 일시정지", bind=toggle_run)
button(text="초기화", bind=reset)
button(text="각도별 주기 비교", bind=run_angle_sweep)
scene.append_to_caption("\n")


def crossing_measurement(previous, current, previous_time, step):
    global last_crossing, measured_period, crossing_count
    if previous > 0 >= current and omega < 0:
        fraction = previous / (previous - current)
        crossing_time = previous_time + fraction * step
        if last_crossing is not None:
            measured_period = crossing_time - last_crossing
            update_readout()
        last_crossing = crossing_time
        crossing_count += 1


refresh_scene()
while True:
    rate(240)
    if not running:
        continue

    step = DT * speed
    previous_theta = theta
    previous_time = sim_time
    angular_acceleration = -gravity / length * sin(theta) - damping * omega
    omega += angular_acceleration * step
    theta += omega * step
    sim_time += step
    bob.pos = bob_position(theta)
    rod.axis = bob.pos
    path.append(pos=bob.pos)
    angle_trace.plot(sim_time, degrees(theta))
    crossing_measurement(previous_theta, theta, previous_time, step)
    if sim_time > 60:
        angle_trace.delete()
        sim_time = 0.0
        angle_trace = gcurve(graph=plot, color=vector(0.25, 0.8, 0.72), width=2)