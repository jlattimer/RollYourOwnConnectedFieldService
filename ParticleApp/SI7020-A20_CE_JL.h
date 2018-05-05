#include "spark_wiring_usbserial.h"
#include "spark_wiring_i2c.h"
#include "spark_wiring_constants.h"

class SI7020_A20_CE_JL {
public:
    double temperatureC();
    double temperatureF();
    float humidity();
    uint8_t getHeater();
    void setHeater(uint8_t);
    void setHeaterPower(uint8_t);
    uint8_t getHeaterPower();
    void turnHeaterOn();
    void turnHeaterOff();
    int resetSensor();
private:
    int address = 0x40;
    int temperatureRegister = 0xE3;
    int humidityRegister = 0xE5;
    int getRawTemperatureReading();
    int getRawHumidityReading();
    uint8_t readRegister(uint8_t);
    void writeRegister(uint8_t, uint8_t);
    int writeUserRegister1 = 0xE6;
    int readUserRegister1 = 0xE7;
    int resetRegister = 0xFE;
    int heaterStatusOff = 58;
    int heaterStatusOn = 62;
    void locationCallback(float, float, float);
};
