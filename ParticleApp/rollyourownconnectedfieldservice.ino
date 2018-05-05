// This #include statement was automatically added by the Particle IDE.
 #include <google-maps-device-locator.h>

SYSTEM_THREAD(ENABLED); // Makes the code run in threaded mode

// Add the temperature sensor library
#include "SI7020-A20_CE_JL.h"
#include "math.h"

SerialLogHandler logHandler;

// Create a sensor object
SI7020_A20_CE_JL sensor;

// Create a Google locator object
GoogleMapsDeviceLocator locator;

// Declare a variable to store temperature readings in
double tempF = 0;
// Declare a variable to store humidity readings in
float humid = 0;
//Declare a varibale to store battery charge
int charge = 0;

// setup() only runs once, when the device is first powered on
void setup() {
    Serial.begin(9600);
    // Function to turn on heater
    Particle.function("heateron", heaterOn);
    //Function to turn off heater
    Particle.function("heateroff", heaterOff);
    //Get heater level
    Particle.function("getheatlevel", getHeatLevel);
    //Set heater level
    Particle.function("setheatlevel", setHeatLevel);
    //Get temperature
    Particle.function("gettemp", getTemp);
    //Get humidity
    Particle.function("gethumid", getHumid);
    //Reset
    Particle.function("reset", resetDevice);
    //Get battery charge percent
    Particle.function("getcharge", getChargePercent);

    // Delay in sec
    locator.withSubscribe(locationCallback).withLocatePeriodic(60);
}

// loop() runs again and again as fast as it can
void loop() {

	// Read the temperature sensor
    tempF = sensor.temperatureF();
    // Round the Tempature down to the nearest whole number
    int wholeNumberTemperature = floor(tempF + .5);

    //Read the humidity sensor
    humid = sensor.humidity();
    // Round the Humidity down to the nearest whole number
    int wholeNumberHumidity = floor(humid + .5);

    charge = getChargePercent("");

    String data =  String(wholeNumberTemperature) + "|" + String(wholeNumberHumidity) + "|" + String(charge);
    Particle.publish("t|h|c", data, 60, PRIVATE);

    locator.loop();

    // Delay in ms
    delay(60000);
}

void locationCallback(float lat, float lon, float accuracy) {
    String data =  String(lat) + "|" + String(lon) + "|" + String(accuracy);
    Particle.publish("location", data, 60, PRIVATE);
}

int heaterOn(String command) {
    sensor.turnHeaterOn();
    Particle.publish("Heater", "On", 60, PRIVATE);
    return 0;
}

int heaterOff(String command) {
    sensor.turnHeaterOff();
    Particle.publish("Heater", "Off", 60, PRIVATE);
    return 0;
}

int getHeatLevel(String command) {
    uint8_t power = sensor.getHeaterPower();
    Particle.publish("Heater Level Currently", String(power) + "mA", 60, PRIVATE);
    return power;
}

int setHeatLevel(String command) {
    int power = command.toInt();

    if (power < 1 || power > 6) {
        Particle.publish("Invalid Heater Level", "1-6 Only", 60, PRIVATE);
        return 1;
    }

    sensor.setHeaterPower(power);
    delay(500);
    power = sensor.getHeaterPower();
    Particle.publish("Heater Level Now", String(power) + "mA", 60, PRIVATE);
    return 0;
}

int getTemp(String command) {
    // Read the temperature sensor
    tempF = sensor.temperatureF();
    // Round the Tempature down to the nearest whole number
    int wholeNumberTemperature = floor(tempF + .5);

    return wholeNumberTemperature;
}

int getHumid (String command) {
    //Read the humidity sensor
    humid = sensor.humidity();
    // Round the Humidity down to the nearest whole number
    int wholeNumberHumidity = floor(humid + .5);

    return wholeNumberHumidity;
}

int resetDevice(String command) {
    int result = sensor.resetSensor();
    delay(15);
    Particle.publish("Reset Device", "", 60, PRIVATE);
    return result;
}

int getChargePercent(String command) {
    FuelGauge fuel;
    return fuel.getSoC();
}
