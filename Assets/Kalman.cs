using System;

public class KalmanFilter {
    private double Q; // Process noise
    private double R; // Measurement noise
    private double P; // Estimation error
    private double K; // Kalman gain
    private double value; // Filtered measurement
    private bool initialized; // Indicates if the filter has been initialized

    private double outlierThreshold; // Threshold for detecting outliers

    public KalmanFilter(double processNoise, double measurementNoise, double estimatedError, double outlierThreshold) {
        Q = processNoise;
        R = measurementNoise;
        P = estimatedError;
        initialized = false;
        this.outlierThreshold = outlierThreshold;
    }

    public double Update(double measurement) {
        if (!initialized) {
            // Initialize the filter with the first measurement
            value = measurement;
            initialized = true;
            return value;
        }

        // Prediction update
        Predict();

        // Measurement update
        K = P / (P + R);
        value = value + K * (measurement - value);
        P = (1 - K) * P + Q;

        return value;
    }

    private void Predict() {
        // Example prediction step (you can customize this based on your model)
        double predictedValue = value * 1.02; // Example: assuming a 2% increase
        P = P + Q; // Update estimation error
        value = predictedValue;
    }

    public bool CheckOutlier(double measurement) {
        return Math.Abs(measurement - value) > outlierThreshold;
    }

    public void SetOutlierThreshold(double threshold) {
        outlierThreshold = threshold;
    }
}
